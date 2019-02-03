from System import *

from Deadline.Events import *
from Deadline.Scripting import *

import requests
import json

_VALID_TASK_STATES = ['Queued', 'Rendering']

def GetDeadlineEventListener():
    return AzureAutoScale()

    
def CleanupDeadlineEventListener(eventListener):
    eventListener.Cleanup()


class AzureAutoScale (DeadlineEventListener):
    def __init__(self):
        self.OnJobSubmittedCallback += self.ScalePoolForJob
        self.OnJobImportedCallback += self.ScalePoolForJob
        self.OnJobRequeuedCallback += self.ScalePoolForJob
        self.OnJobResumedCallback += self.ScalePoolForJob
    
    def Cleanup(self):
        del self.OnJobSubmittedCallback
        del self.OnJobImportedCallback
        del self.OnJobRequeuedCallback
        del self.OnJobResumedCallback

    def ScalePoolForJob(self, job):
    
        enabledPools = self.GetConfigEntryWithDefault("EnabledPools", "").split(";")
        enabledGroups = self.GetConfigEntryWithDefault("EnabledGroups", "").split(";")

        if not enabledPools and not enabledGroups:
            self.LogInfo('AutoScale: No pools or groups specified in plugin configuration, exiting.')
            return
            
        if job.JobStatus == "Suspended":
            self.LogInfo('AutoScale: Ignoring suspended job {}'.format(job.JobName))
            return

        targetPool = None
        totalPendingTasks = job.JobTaskCount
        if job.JobPool in enabledPools:
            targetPool = job.JobPool
        elif job.JobGroup in enabledGroups:
            targetPool = job.JobGroup
            
        self.LogInfo('AutoScale: Received event for job {} ({}) with pool {} and group {}'.format(
            job.JobName, job.JobStatus, job.JobPool, job.JobGroup))

        if targetPool:
            jobs = RepositoryUtils.GetJobsInState('Active')
            for j in jobs:
                if j.JobId == job.JobId:
                    # Skip the current job
                    self.LogInfo('AutoScale: Skipping current Job {} ({})'.format(j.JobName, j.JobStatus))
                    continue
                if j.JobPool == targetPool:
                    taskCollection = RepositoryUtils.GetJobTasks(j, True)
                    jobTasks = 0
                    for t in taskCollection.TaskCollectionAllTasks:
                        if t.TaskStatus in _VALID_TASK_STATES:
                            jobTasks += 1
                            
                    self.LogInfo('AutoScale: Job {} ({}) has {} incomplete tasks'.format(j.JobName, j.JobStatus, jobTasks))
                    totalPendingTasks += jobTasks
                            
        self.LogInfo('AutoScale: Requesting {} nodes'.format(totalPendingTasks))
        
        if targetPool and totalPendingTasks > 0:
            self.ScalePool(targetPool, totalPendingTasks)
    
    def ScalePool(self, poolName, requestedNodes):
        baseUrl = self.GetConfigEntry("EnvironmentUrl")
        key = self.GetConfigEntry("EnvironmentKey")
        
        data = {"requestedNodes": requestedNodes}
        data_json = json.dumps(data)
        headers = {'Content-type': 'application/json', 'Authorization': 'Basic {}'.format(key)}
        url = '{}/pools/{}'.format(baseUrl, poolName)
        response = requests.post(url, data=data_json, headers=headers)
        self.LogInfo('AutoScale: Got Response {}'.format(response))

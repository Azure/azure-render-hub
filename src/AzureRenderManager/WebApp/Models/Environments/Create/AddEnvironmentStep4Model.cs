// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.ComponentModel.DataAnnotations;

using WebApp.Config;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentStep4Model : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public AddEnvironmentStep4Model() { }

        public AddEnvironmentStep4Model(RenderingEnvironment environment)
        {
            EnvironmentName = environment.Name;
            RenderManager = environment.RenderManager;
            JoinDomain = environment.Domain?.JoinDomain ?? false;
            DomainName = environment.Domain?.DomainName;
            DomainWorkerOuPath = environment.Domain?.DomainWorkerOuPath;
            DomainJoinUsername = environment.Domain?.DomainJoinUsername;
            DomainJoinPassword = environment.Domain?.DomainJoinPassword;

            switch (RenderManager)
            {
                case RenderManagerType.Deadline:
                    DeadlineEnvironment = new DeadlineEnvironment
                    {
                        WindowsDeadlineRepositoryShare = environment.RenderManagerConfig?.Deadline?.WindowsRepositoryPath,
                        RepositoryUser = environment.RenderManagerConfig?.Deadline?.RepositoryUser,
                        RepositoryPassword = environment.RenderManagerConfig?.Deadline?.RepositoryPassword,
                        InstallDeadlineClient = environment.RenderManagerConfig?.Deadline?.LicenseServer != null,
                        DeadlineRegion = environment.RenderManagerConfig?.Deadline?.DeadlineRegion,
                        ExcludeFromLimitGroups = environment.RenderManagerConfig?.Deadline?.ExcludeFromLimitGroups,
                        LicenseMode = environment.RenderManagerConfig?.Deadline?.LicenseMode,
                        LicenseServer = environment.RenderManagerConfig?.Deadline?.LicenseServer,
                        RunAsService = environment.RenderManagerConfig?.Deadline?.RunAsService ?? false,
                        ServiceUser = environment.RenderManagerConfig?.Deadline?.ServiceUser,
                        ServicePassword = environment.RenderManagerConfig?.Deadline?.ServicePassword,
                    };
                    break;

                case RenderManagerType.Qube610:
                case RenderManagerType.Qube70:
                    QubeEnvironment = new QubeEnvironment
                    {
                        QubeSupervisor = environment.RenderManagerConfig?.Qube?.SupervisorIp,
                    };
                    break;

                case RenderManagerType.Tractor:
                    TractorEnvironment = new TractorEnvironment
                    {
                        TractorSettings = environment.RenderManagerConfig?.Tractor?.TractorSettings
                    };
                    break;
            }
        }

        // TODO: Reference the following with a single base abstract model

        public DeadlineEnvironment DeadlineEnvironment { get; set; }

        public QubeEnvironment QubeEnvironment { get; set; }

        public TractorEnvironment TractorEnvironment { get; set; }

        public bool JoinDomain { get; set; }

        public string DomainName { get; set; }

        public string DomainWorkerOuPath { get; set; }

        public string DomainJoinUsername { get; set; }

        public string DomainJoinPassword { get; set; }
    }
}

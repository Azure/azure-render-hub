# Auto Scale Down

Render Hub has the ability to automatically scale pools down when nodes (virtual machines) become idle.  This can significantly reduce costs and the risk that unused nodes are left running.  The Auto Scale Down only scales pools down, not up.  The latter requires deeper integration with render managers such as Deadline or Qube.  For more information on plugins for scaling up please contact rendering@microsoft.com.

Auto scale down relies on CPU, GPU and process telemetry emitted from each render node.  Render Hub install an Application Insights agent (Batch Insights) on each node.  CPU, GPU and rendering process telemetry is sent to Application Insights.

For more information on Batch Insights see [here](https://github.com/Azure/batch-insights).

Using the above telemetry, Render Hub evaluates each node to see if it has exceeded the configured idle period at which point it is removed from the Pool.

## Configuration

Each Pool can specify an Auto Scale down policy from the following.

. Disabled - No nodes will be removed from the pool.
. Resources (CPU and GPU) - Nodes are determined idle if CPU or GPU falls below the value specified in the Environment config.
. Specific Processes - Nodes are determined idle if no known rendering processes are running on the node.
. Resources + Specific Processes - The node must be below the CPU and GPU idle percent and have no running rendering processes.

### Idle Threshold

The CPU and GPU idle percentage can be specified in the Environment -> Configuration page.  The default avlues are 5% and 2% respectively.

### Specific Processes

Render Hub includes a list of known rendering processes that it watches for on the nodes.  This list can be extended with any process names by specifying them on the Environment -> Configuration page.

The default processes include (and their Linux alternatives where applicable):

3dsmax.exe, 3dsmaxcmd.exe, 3dsmaxio.exe, 3dsmaxcmdio.exe, render.exe, kick.exe, commandline.exe, cinema 4d.exe, vray.exe, maya.exe, mayabatch.exe, blender.exe

### Pool Auto Scale Configuration

The Auto Scale down policy can be enabled on each pool where a timeout can be set.  The minimum timeout is 15 minutes.

#### Minimum number of nodes

Each pool can specify the minimum number of nodes in the pool.  This value affects auto scale only, it is still possible to manually scale the pool below this level.

#### Maximum number of nodes

This value specifies the maximum nodes the pool can scale to via the Scale Up API to ensure 3rd party plugins don't exceed safe limits.  The pool can still be manually scalled beyond this value.

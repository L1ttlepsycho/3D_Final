# AI坦克对战
使用Kawaii Tank 2.0素材实现

*演示视频传送门：*
## 具体要求
1. 使用“感知-思考-行为”模型，建模 AI 坦克
2. 场景中要放置一些障碍阻挡对手视线
3. 坦克需要放置一个矩阵包围盒触发器，以保证 AI 坦克能使用射线探测对手方位
4. AI 坦克必须在有目标条件下使用导航，并能绕过障碍。（失去目标时策略自己思考）
5. 实现人机对战

## 大致思路
使用一个简单的状态机作为AI行为控制器，使用素材中自带的地形进行Navmesh，达到自动寻路的效果，AI通过一个Trigger进行玩家方位探测，瞄准则是通过视觉（即射线），通过两方面探测进行对玩家的追踪与攻击。

## 实现细节
### 资源获取
在Unity资源商店中直接搜索Kawaii Tank，下载并且导入资源。

### 玩家实现
玩家实现基本照搬资源上的东西，只要选择一个玩家坦克（我选择了虎式），按照资源例子场景挂载摄像机和控制就行了。

### AI实现
我选用了Firefly Tank，AI的实现就要稍微下一点功夫了，因为不仅涉及到AI寻路，还涉及AI的瞄准与开火。

**AI的感知：**
1. 视觉确定玩家的具体方向，以便瞄准射击。
2. 声音确定玩家的大致方位，以便追击玩家。

**AI的思考**

使用了简单的状态机作为AI的大脑

**AI的行动**

利用原有的脚本修改而来，AI只要装填好子弹就会进行射击。

*AI行动的状态机如下：*

![SVM]()



**寻路与巡逻**

寻路比较简单，可以参照standard asset中ThirdPersonAI的脚本进行编写
```
public class AIBehaviour : MonoBehaviour
    {
        public UnityEngine.AI.NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
        public Transform tank { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
        public Aiming_Control_CS aim;
        public List<Vector3> patrol_route = new List<Vector3>(4);
        private bool on_chase;
        private int pos_index = 0;

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            tank = this.transform.parent;
            aim = GetComponent<Aiming_Control_CS>();
            agent.updateRotation = true;
            agent.updatePosition = true;
            on_chase = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.tag == "Player")
            {
                agent.stoppingDistance = 15;
                //Debug.Log("chasing... ");
                target = other.transform;
                on_chase = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.tag == "Player")
            {
                if (on_chase)
                    on_chase = false;
                //Debug.Log("losing target...");
                target = null;
                on_chase = false;
                agent.stoppingDistance = 0;
            }
        }

        private void Update()
        {
            if (tank.tag == "AI")
            {
                if (isInSight())
                {
                    Debug.Log("in range");
                    Transform turret = this.transform.Find("Turret_Objects");
                    aim.targetPosition = target.position;
                    aim.targetTransform = target;
                }
                if (on_chase)
                {
                    if (target != null)
                    {
                        agent.SetDestination(target.position);
                    }
                }
                else
                {
                    if (agent.remainingDistance == agent.stoppingDistance)
                    {
                        pos_index = (pos_index + 1) % 4;
                        agent.SetDestination(patrol_route[pos_index]);
                    }

                }
            }
            else
            {
                agent.isStopped = true;
            }
        }

        public bool isInSight()
        {
            if (tank.tag == "AI" && target != null)
            {
                Vector3 perspect = tank.position + new Vector3(0, 2, 0);
                Ray ray = new Ray(perspect, target.position - perspect);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    //Debug.Log(hit.collider.name);
                    if (hit.collider.tag == "Player")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void SetRoute(List<Vector3> route)
        {
            for (int i = 0; i < 4; i++)
            {
                patrol_route[i] = route[i];
            }
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }
    }
```
代码基本上就是对进入侦察范围的玩家进行追踪，但比较特别的是```isInSight()```这个函数，用于判断玩家是否处在AI的视野里。在就返回true，意味着AI可以瞄准玩家了。

将上面编写好的脚本、一个Trigger（可以是box也可以是sphere）与主角Navmesh一起挂载在AI坦克的mainbody上。

Navmesh调参如下：

![Inspector]()

由于NavMeshSurface不具有我需要的某些特性，比如能够设定距离目标多少米就停下，于是没有使用NavMeshSurface，不然就能够在游戏中实现树木被毁坏后更新AI导航了。

**瞄准**

在游戏里瞄准涉及到了炮管的升降与炮台的旋转，自己实现过于繁琐，而且效果也不理想，于是我们需要对素材中玩家瞄准的脚本Aiming_Control进行利用

Aim_Control与其他有关脚本的联动

![关系图]()

通过阅读代码我们发现，只需要设置Aiming_Control中的Target有关变量，就可以实现AI的瞄准。

就是他俩
```
[HideInInspector] public Vector3 targetPosition; // Referred to from "Turret_Control_CS", "Cannon_Control_CS", "AimMarker_Control_CS".
[HideInInspector] public Transform targetTransform; // Referred to from "AimMarker_Control_CS".
```

**射击**

射击这里采用了简化的处理方式，让AI只要炮弹填装完毕就进行射击，稍微修改一下Fire_Control脚本的```Update()```函数.
```
        void Update()
        {
            if (isLoaded == false)
            {
                return;
            }

            if(thisTransform.parent.parent.parent.parent.tag == "Player")
                inputScript.Get_Input();
            else
            {
                Fire();
            }
        }
```

## 最终效果
见演示视频，可以考虑给AI坦克添加射击随机散布，它打玩家有些太准了。

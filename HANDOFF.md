公开版说明：仅包含两套许可已确认的CC0素材原件；四个OBJ包只保留索引，不提供原件。

# 天空岛：渲染与建模交接包

基线提交：3afd25ab355865ee8a062b6cc4b113b0f18a42a1
引擎：Unity 2022.3.62f3c1（中国版），Built-in管线。打开UnityProject。离线建造原型，当前网页并非此源码最新版本。包不含Library/Temp/Build/.git、账号凭据或玩家存档，首次打开需Unity重新导入。依赖清单随工程，联网解析情况按实际环境报告。

## 可直接粘贴给另一位GPT的任务
请读取本包真实工程，专门优化视觉、渲染和建模。先给出视觉基线截图，再实现一小批能在Unity中运行的改进并提交回传包。重点是风格统一、轻量风格化3D、无缝六边形浮岛、角色与三种宠物、树石草与模块化木板建筑、泉水与小飞艇/船坞。先完成岛体边缘/地表、天空云层、材质光照的一批对比，不同时重做所有模型。保留当前动作、尺度、碰撞、坐标、地基128上限、木板支撑/旋转和存档语义。

## 现在真实状态
主场景Assets/SkyIsland/Scenes/Prototype.unity；大部分对象运行时由SkyIslandPrototype及partial脚本生成，因此单改编辑器场景不一定改变实际画面。创建角色、岛体、云和材质的入口在Scripts/中，先按符号检索确认。已有ExternalLibrary自然素材与GroundDetails预制件，后者尚未全部铺入主岛。当前角色与宠物仍是灰盒；背包27+8已在本地实现。FoundationClient/Authority是独立权威验证，尚未接入主游戏实时联机。

## 与Codex并行的修改边界
Codex继续改联机、背包、经济与存档。新增文件统一放Assets/SkyIsland/ArtUpgrade/（含Models/Materials/Shaders/Prefabs/Editor）。不要直接交付一套覆盖整个主工程的旧副本。现有SkyIslandPrototype.cs、SurvivalGameplay.cs、AdventureGameplay.cs、Nature*.cs等共享脚本如需接线，提交单独INTEGRATION.patch或精确方法级修改说明；Codex按最新主线人工合并。不要修改Authority/、网络协议、背包、采集掉落、存档字段、输入按键或奖励参数。不要更改Packages、ProjectSettings或渲染管线；URP仅可在独立副本实验并给出差异与回退，主线迁移仍待用户确认。

## 已有素材怎么用
AssetLibrary/raw按原六压缩包类目保留源文件；Reference/tracking/asset-library.json为索引，共399条（重复家具包保留分类）。原索引中的路径以原仓库为根，本包相应映射到AssetLibrary/raw；导入示例脚本作参考，不能直接把本包当原仓库运行。UnityProject/Assets/ExternalLibrary是已导入的子集。自然标准包和KayKit此具体包随附CC0许可；四个OBJ包来源仍待核实，仅可测试，发布场景暂不使用。不要把整个库放Resources，不重复导入OBJ/FBX/glTF多份。保留源许可和来源，新引入资产写入来源清单。

## 验收与预算
保持API26兼容目标；代表设备中位FPS≥45、P5≥30、纹理峰值≤128MB、最终Android发布包≤60MB。以上是目标，不是当前已测结果。先记录新旧相同场景、分辨率、相机、对象数、三角形、材质/纹理、Draw Calls、CPU/GPU帧时间；无设备时明确未验证。建模单位米，Y向上，根缩放1，原点与碰撞明确，尽量低材质数/共享图集；角色动画骨架、LOD和碰撞要单独留证。不要用截图替代实跑，也不要把成功编译写成真机性能通过。

## 回传格式
1. RETURN/changed-files/ 下仅放新增或修改文件（相对UnityProject根，保留.meta/GUID）。新文件须配.meta。
2. RETURN/CHANGELOG.md：基线提交、修改目的、完整文件清单、删除清单（如有）、共享脚本接线说明、已知限制。
3. RETURN/ASSETS.json：来源/许可、比例/尺寸、三角形、材质、贴图、LOD、碰撞、prefab路径；AI生成记录工具/参数与人工处理。
4. RETURN/EVIDENCE/：真实Unity编译日志、同视角前后截图、实际测试命令及结果；手机/性能没测就写没测。
5. 对现有共享脚本的改动单独给INTEGRATION.patch，勿整文件覆盖。UnityProject/ProjectSettings、Packages的实验差异只能作候选说明。
6. 打成zip发回用户，由Codex在主线比对GUID、检查来源、回归碰撞与存档、构建后集成。不要自行发布网站或部署后端。

本包MANIFEST.json给出逐文件SHA256；根目录包验收报告为打包完整性验证，不是美术验收。

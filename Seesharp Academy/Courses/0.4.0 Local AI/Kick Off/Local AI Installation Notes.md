### LLM Studio

* LLM Studio网页

C:\Users\hshao1\.lmstudio

* 大模型位置手工在json配置

  * 默认 C:\Users\[你的用户名]\.lmstudio\models

  * 修改：

    * 完全关闭 LM Studio（包括后台 `lmstudio-server`）

      打开配置文件路径：

      plaintext

      ```
      %APPDATA%\LMStudio\config.json
      ```

      在 `config.json` 中添加 / 修改 `modelPath` 字段，指定新路径：

      json

      ```
      {
        "modelPath": "D:\\LMStudioModels",
        "其他配置项": "..."
      }
      ```

      - 路径用**双反斜杠** `\\` 或**单正斜杠** `/`
      - 确保目标文件夹已存在（如 `D:\LMStudioModels`）

      保存文件，重启 LM Studio

      新下载的模型会自动存到指定路径

  * 已下载模型迁移
    1. 关闭 LM Studio
    2. 把原路径 `C:\Users\[你的用户名]\.lmstudio\models` 整个文件夹**剪切 / 复制**到新路径（如 `D:\LMStudioModels`）
    3. 按上面方法修改 `config.json` 的 `modelPath`
    4. 重启 LM Studio，即可识别已迁移的模型

* 大模型load和启动，会自动配置context大小，充分使用GPU内存

### Anything LLM

* 官网

  [AnythingLLM | The all-in-one AI application for everyone](https://anythingllm.com/)

* 资料库位置
  * 缺省在 C:\Users\[你的用户名]\AppData\Roaming\anythingllm-desktop\storage
  * 需要用 符号链接 技术将此目录转移到新的目录

### 个案消耗

LLM Studio + 8B模型约 10GB

Anything LLM安装要求 1.3GB，实际观察大约9GB 这些远超安装大小的消耗，在关闭软件后消失，估计是Anything LLM运行中的临时文件





### 运行

1. LLM Studio >> Developer >> Status切到Running
2. Anything LLM >>启动配置LM Studio
   1. URL用 http://localhost:1234/v1，不能用127.0.0.1 (报URL错)
3. 正常点 -》箭头就完成了启动配置
4. 简单的工作流程：
   1. **创建工作区**：用于建立独立的知识库空间
   2. **嵌入文档**：上传 PDF/Word/ 文本等文件，构建本地知识库
   3. **发送聊天**：与 AI 进行对话，支持基于知识库问答
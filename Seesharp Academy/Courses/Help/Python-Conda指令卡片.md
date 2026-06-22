### Conda环境

安装Anaconda或Miniconda管理Python运行环境。会在Conda目录建立你自己项目的一套Python软件环境。在VS Code中Ctrl-Shift-P选择Python:Select Interpreter可以选到你的环境。

推荐：MiniConda占用硬盘显著小

### Anaconda Promt常用指令

| 操作                              | 指令                                |
| --------------------------------- | ----------------------------------- |
| 查看所有环境                      | conda env list                      |
| 创建环境（Python3.14）            | conda create -n 环境名 python=3.14  |
| 激活环境                          | conda activate 环境名               |
| 退出环境                          | conda deactivate                    |
| 安装numpy/pandas                  | conda install numpy pandas          |
| 删除环境                          | conda remove -n 环境名 --all        |
| 生成requirements.txt(pip安装列表) | pip freeze > requirements.txt       |
| 安装requirements.txt              | pip install -r requirements.txt     |
| anaconda导出完整环境(带名称)      | conda env export > environment.yml  |
| anaconda恢复环境                  | conda env create -f environment.yml |


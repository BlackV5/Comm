# Comm 串口调试工具

[![.NET Core Desktop](https://github.com/BlackV5/Comm/actions/workflows/Comm.yml/badge.svg)](https://github.com/BlackV5/Comm/actions/workflows/Comm.yml)
[![GitHub release](https://img.shields.io/github/release/BlackV5/Comm.svg)](https://github.com/BlackV5/Comm/releases)
[![GitHub downloads](https://img.shields.io/github/downloads/BlackV5/Comm/total.svg)](https://github.com/BlackV5/Comm/releases)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Comm 是一个基于 .NET Framework 4.7.2 开发的 WPF 串口调试工具，提供了简洁直观的串口通信调试界面，适用于嵌入式开发、硬件调试等场景。

---

## ✨ 功能特性

### 🔌 串口通信
- 串口打开/关闭
- 实时数据收发
- 自动检测可用串口

### ⚙️ 参数配置
- 波特率：多种常用波特率可选
- 数据位：5/6/7/8
- 停止位：1/1.5/2
- 校验位：无/奇/偶/标记/空格
- 流控制：无/硬件/软件

### 📤 数据发送
- 支持 **字符串** 格式发送
- 支持 **HEX** 格式发送
- 支持 **自动换行**、**定时发送**
- 发送历史记录

### 📥 数据接收
- 实时显示接收数据
- 支持 **字符串** / **HEX** / **ASCII** 多种显示格式
- 接收数据自动换行
- 接收时间戳显示
- 支持清空接收区

### 🚀 快捷发送
- 预置常用指令（可自定义编辑）
- 一键快速发送
- 支持快捷指令管理（增加、删除、修改）

### 💾 配置管理
- 自动保存串口配置
- 下次启动自动加载
- 窗口位置记忆

---

## 📥 下载与安装

### 方式一：直接下载 EXE（推荐普通用户）

1. 访问 [Releases 页面](https://github.com/BlackV5/Comm/releases)
2. 下载最新版本的 `Comm_vX.X.X.exe`
3. 双击运行即可（**无需安装，绿色便携**）

### 方式二：从源码构建（推荐开发者）

```bash
# 克隆仓库
git clone https://github.com/BlackV5/Comm.git
cd Comm

# 使用 Visual Studio 2022 打开 Comm.sln
# 或使用 MSBuild 命令行构建
msbuild Comm.sln /p:Configuration=Release /p:Platform="Any CPU"
[![](https://badge.mcpx.dev?status=on 'MCP Enabled')](https://modelcontextprotocol.io/introduction)
[![](https://img.shields.io/badge/Unity-000000?style=flat&logo=unity&logoColor=white 'Unity')](https://unity.com/releases/editor/archive)
[![](https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white 'C#')](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![](https://img.shields.io/github/last-commit/NoSpoonLab/unity-mcp 'Last Commit')](https://github.com/NoSpoonLab/unity-mcp/commits/main)
[![](https://img.shields.io/badge/License-MIT-red.svg 'MIT License')](https://opensource.org/licenses/MIT)

# Unity MCP Server (C#)

This project is a Model Context Protocol (MCP) server for Unity, providing a bridge between the Unity Editor and external Large Language Models (LLMs) or cloud-based AI agents. The server side is fully implemented with C#.

## What is Unity Model Context Protocol (MCP)?

Unity MCP is a protocol designed to enable seamless communication between the Unity Editor and external tools, scripts, or AI models. It allows for real-time automation, remote control, and intelligent interaction with Unity projects. MCP can be used as a bridge so that LLMs (Large Language Models), either running locally or in the cloud, can directly interact with the Unity Editor—enabling advanced workflows, procedural content generation, automated testing, and more.

## Key Features

- **C# Server Implementation**: The backend/server is written entirely in C#, making it easy to integrate with Unity and .NET environments.
- **MCP Bridge**: Acts as a bridge between Unity and external LLMs or cloud services, allowing AI models to send commands and receive data from the Unity Editor.
- **Real-time Automation**: Supports real-time automation of editor tasks, scene manipulation, asset management, and more.
- **Extensible Protocol**: Built on the open Model Context Protocol, making it easy to extend for custom workflows or new AI capabilities.
- **Inspired by**: This project is based on the original work at [https://github.com/justinpbarnett/unity-mcp/](https://github.com/justinpbarnett/unity-mcp/).

## How it Works

1. **Server-Client Architecture**: The C# MCP server listens for incoming connections from clients (such as LLMs, scripts, or cloud agents).
2. **Command Handling**: Clients send MCP-formatted messages to the server, which are then interpreted and executed in the Unity Editor context.
3. **Bi-directional Communication**: The server can send responses, data, or events back to the client, enabling interactive and intelligent workflows.
4. **Use Cases**: Procedural content generation, automated scene setup, AI-driven testing, remote Unity control, and more.

---

This project is a starting point for anyone looking to connect Unity with the power of LLMs or external automation tools using the Model Context Protocol. 
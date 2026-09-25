<div align="center">

# ⚡ Swifter

### The Ultimate Enterprise-Grade Web Browser

**Blazing fast. Infinitely customizable. Built for power users.**

[![Build & Release](https://github.com/nirob100bd/swifter/actions/workflows/build-release.yml/badge.svg)](https://github.com/nirob100bd/swifter/actions/workflows/build-release.yml)
[![Latest Release](https://img.shields.io/github/v/release/nirob100bd/swifter?label=Latest%20Release&logo=github)](https://github.com/nirob100bd/swifter/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/nirob100bd/swifter/total?logo=github&label=Downloads)](https://github.com/nirob100bd/swifter/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-blue?logo=windows)](https://github.com/nirob100bd/swifter/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

<a href="https://github.com/nirob100bd/swifter/releases/latest/download/Swifter_Setup_v1.0.0.exe">
  <img src="https://img.shields.io/badge/⬇️_Download_Swifter_Setup_v1.0.0.exe-blue?style=for-the-badge&logo=github&logoColor=white" alt="Download Swifter" height="50">
</a>

**👉 [Click here to download the latest release](https://github.com/nirob100bd/swifter/releases/latest)** 👈

</div>

---

## 🚀 What is Swifter?

Swifter is a **hyper-optimized, ultra-responsive desktop web browser** built with C# (.NET 8), WPF with Fluent UI/Mica glass aesthetics, and Microsoft WebView2. It's designed from the ground up for **maximum performance** on modern AMD Ryzen and Intel multi-core processors.

---

## ✨ Key Features

### 🏎️ Performance & Speed
- **Sub-millisecond window dragging** via native Win32 message interception
- **AVX2/SSE4.2 vectorized** network buffer pipelines
- **32-stream parallel downloader** with automatic segment rebalancing
- **Aggressive tab hibernation** and memory reclamation
- **Ryzen & Intel optimized** thread affinity and CPU scheduling

### 🎨 Beautiful Interface
- **Fluent UI / Mica glass** composition with hardware-accelerated transparency
- **Dark & Light themes** with customizable accent colors
- **Compact mode** for maximum screen real estate
- **Smooth animations** throughout the interface

### 🛡️ Privacy & Security
- **Aho-Corasick ad & tracker blocking** engine
- **Encrypted password vault** with Windows DPAPI + AES-256
- **HTTPS-only mode** with DNS over HTTPS support
- **Multi-account sandbox** for isolated profiles
- **WebRTC leak protection** and fingerprint blocking

### ⚡ Power-User Features
- **Mouse gestures** for instant navigation
- **Boss Key** (Win+Z) — instantly hide browser with Excel overlay
- **Command Palette** (Ctrl+Shift+P) for lightning-fast actions
- **Tab stacking & grouping** with color codes
- **Split view** for side-by-side browsing
- **Picture-in-Picture** for video popout
- **Focus Mode** for distraction-free reading
- **Reader Mode** for clean article layouts
- **QR code generator** for instant mobile sharing

### 📥 Media & Downloads
- **Universal media sniffer** for YouTube, Vimeo, TikTok, Twitch, and more
- **Resolution picker** for video downloads (1080p, 720p, audio)
- **BitTorrent P2P streaming** directly in browser tabs
- **FTP/SFTP file browser** built in

### 🔧 Developer Tools
- **Built-in DOM inspector** and network console
- **WebSocket debugger** for real-time message inspection
- **CSS style auditor** and element selector
- **Custom extension** loading system

### 📊 Productivity
- **RSS feed reader** with background polling
- **Web snippet clipper** for saving content
- **Floating notes overlay** for quick capture
- **Canvas whiteboard** for sketching ideas
- **Quick calculator** in the address bar
- **Page translator** overlay
- **Workspace management** for organizing tab groups

---

## 💻 System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **OS** | Windows 10 (x64) | Windows 11 |
| **CPU** | Any x64 processor | AMD Ryzen 5 / Intel i5+ |
| **RAM** | 4 GB | 8 GB+ |
| **Storage** | 200 MB | 500 MB+ |
| **Runtime** | .NET 8.0 | .NET 8.0 |
| **WebView2** | Auto-installed | Pre-installed |

---

## 📥 Installation

### Option 1: Download Installer (Recommended)

1. **[Download Swifter_Setup_v1.0.0.exe](https://github.com/nirob100bd/swifter/releases/latest/download/Swifter_Setup_v1.0.0.exe)** from the latest release
2. Run the installer — it creates desktop shortcuts and Start Menu entries
3. Launch Swifter and enjoy!

### Option 2: Build from Source

```bash
git clone https://github.com/nirob100bd/swifter.git
cd swifter
dotnet restore
dotnet build -c Release -r win-x64
dotnet run -c Release -r win-x64
```

---

## ⌨️ Essential Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| New Tab | `Ctrl+T` |
| Close Tab | `Ctrl+W` |
| Reopen Tab | `Ctrl+Shift+T` |
| Address Bar | `Ctrl+L` |
| Reload | `F5` |
| DevTools | `F12` |
| Fullscreen | `F11` |
| Command Palette | `Ctrl+Shift+P` |
| Find on Page | `Ctrl+F` |
| Downloads | `Ctrl+J` |
| History | `Ctrl+H` |
| Bookmarks | `Ctrl+D` |

---

## 🏗️ Architecture

```
Swifter/
├── Core/Native/      → Win32 interop, CPU optimization, DWM composition
├── Core/UI/          → All UI components, animations, engines
├── Core/TabEngine/   → Tab management, hibernation, sessions
├── Core/Config/      → Settings model and persistence
├── Core/Storage/     → SQLite databases for history, bookmarks, vault
├── Core/Network/     → Downloads, media sniffer, shield, proxy, FTP
├── Core/Protocols/   → Custom swifter:// scheme and internal pages
├── Installer/        → Inno Setup installer builder
└── .github/workflows/ → Automated CI/CD build and release pipeline
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

<div align="center">

**Built with ❤️ by [nirob100bd](https://github.com/nirob100bd)**

⚡ **Swifter** — Browse at the speed of thought.

</div>
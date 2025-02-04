# Automated Test Bench for Embedded Systems

## Overview
This project is a **C# WinForms application** that interfaces with **microcontrollers (ESP32, Arduino)** via serial communication. It automates test sequences and includes a Lua-based scripting engine for automated testing.



## Features
- 🔌 Serial communication with **customizable baud rate, parity, and handshake settings**.
- 🔄 **Automated test sequences** that send multiple commands in order.
- 📝 **Lua scripting engine** to define test logic dynamically.
- 🖥️ **ESP32/Arduino firmware** for real hardware interaction.
- 📊 **Logging** of all serial data and execution history.

## How to Use
1. **Connect your device** via USB and select the correct COM port.
2. **Add test sequences** by entering a test name and adding commands.
3. **Run test sequences** automatically.
4. **Use Lua scripting** to write and execute complex test scripts.

## Installation
1. Clone this repository:
   ```sh
   git clone https://github.com/YOUR_GITHUB_USERNAME/AutomatedTestBench.git

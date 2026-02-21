# MONROE – Modular Orchestrator & Router Engine

MONROE is a transparent, modular multi‑agent orchestrator that exposes multiple local or remote AI models through a single OpenAI‑compatible API.  
Its purpose is to provide predictable routing, full transparency, and strict control over how different models are used — without relying on hidden heuristics or black‑box behavior.

> **Status:**  
> MONROE is currently a **work in progress**.  
> The first public code release is planned and will be published soon.  
> This repository is being prepared for the initial commit.

## 🚀 Purpose

Modern AI workflows often require multiple specialized models (coding, vision, creative reasoning, etc.).  
Most frontends, however, only support selecting **one** model at a time.

MONROE solves this by acting as a **single unified model** externally, while internally orchestrating multiple agents based on user intent.

## 🧠 Core Features

- **Single API endpoint**  
  Exposes all models through one OpenAI‑compatible interface.

- **Automatic semantic routing**  
  A lightweight 1B–2B classifier determines which agent should handle each request.

- **Dynamic model loading/unloading**  
  Saves RAM by loading large models only when needed.

- **Transparent decision‑making**  
  Every routing decision is logged and explainable.

- **Modular agent architecture**  
  Each model (local or remote) is encapsulated in its own agent module with its own policies.

- **Fully OpenAI‑compatible**  
  Works with Open WebUI, LM Studio, and any OpenAI client.

## 🏗️ Architecture Overview

- **Router Core**  
  Receives requests, extracts context, and delegates to the appropriate agent.

- **Intent Classifier**  
  A small model that identifies user intent (coding, vision, creative, system tasks, etc.).

- **Agent Modules**  
  Each agent wraps a model and defines its context size, capabilities, and routing rules.

- **Model Manager**  
  Handles loading, unloading, and resource monitoring.

- **Logging Layer**  
  Provides full visibility into routing decisions and model usage.

## 🎯 Project Goals

- Reliable, deterministic routing  
- No keyword‑based triggers  
- No session locking  
- Full transparency and reproducibility  
- Efficient resource usage  
- Easy integration into existing OpenAI‑based workflows

## 📦 Project Status

MONROE is under **active development**.  
The initial codebase is being finalized and will be published soon.  
APIs, routing logic, and module structure may evolve as the project matures.

## 📜 License

This project is licensed under the  
**Creative Commons Attribution‑NonCommercial 4.0 International (CC BY‑NC 4.0)** license.

This means:

- Attribution is required  
- Commercial use is prohibited  
- Modifications and sharing are allowed for non‑commercial purposes  
- Commercial usage requires explicit written permission from the author

See the `LICENSE` file for full details.

## 🤝 Contributions

Contributions are welcome as long as they comply with the license.  
For major changes, please open an issue first to discuss the proposed modification.

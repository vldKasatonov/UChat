# Virma — Project Setup Guide

This README provides step-by-step instructions on how to prepare the environment, create the required Docker container, and run both the server and the client applications.

---

## 🧰 Prerequisites

Before working with this project, ensure the following tools are installed:

### ✔️ .NET 9 SDK

The server and client applications are built using **.NET 9**, so you must have the .NET 9 SDK installed.  
You can download it here:

👉 **[Download .NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)**

To verify installation:

```bash
dotnet --version
```

---

## 📦 1. Install Docker

If Docker is not installed on your machine, download it from the official website:

👉 **[Download Docker Desktop](https://www.docker.com)**

Make sure Docker Desktop is running before moving on.

---

## 🐳 2. Create the Docker Container

The project includes a Docker configuration located at:
[**uchat_server/compose.yaml**](./uchat_server/compose.yaml)

To create and start the container, run the following command **from the project root directory**:

```bash
docker-compose -f uchat_server/compose.yaml up -d
```

---

## 3. Run the Server

Before launching the client, you need to start the server manually.  
Use the following command:

```bash
dotnet run --project uchat_server 8080
```

>This will start the server on port 8080.

---

## 4. Run the Client

Once the server is running, start the client application:

```bash
dotnet run --project uchat 127.0.0.1 8080
```

>This connects the client to the server running locally on port 8080.

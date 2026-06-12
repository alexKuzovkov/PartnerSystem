# \# 🤝 PartnerSystem — Система партнёрских отчислений

# 

# Распределённая микросервисная система для начисления и выплаты партнёрских комиссий в иерархии пользователей. Поддерживает две схемы начисления (Linear и Fibonacci), идемпотентность, Outbox-паттерн и горизонтальное масштабирование.

# 

# \---

# 

# \## 📋 Содержание

# 

# \- \[Архитектура](#-архитектура)

# \- \[Технологический стек](#-технологический-стек)

# \- \[Структура проекта](#-структура-проекта)

# \- \[Быстрый старт](#-быстрый-старт)

# \- \[Unit-тесты](#-unit-тесты)

# \- \[Интеграционные тесты](#-интеграционные-тесты)

# \- \[API Reference](#-api-reference)

# \- \[Архитектурные решения](#-архитектурные-решения)

# 

# \---

# 

# \## 🏗 Архитектура

# 

# \### Общая схема системы

# 

# ```mermaid

# graph TB

# &#x20;   subgraph "Клиенты"

# &#x20;       Client\[HTTP Клиент / Postman]

# &#x20;   end

# 

# &#x20;   subgraph "Микросервисы"

# &#x20;       US\[UserService<br/>:5001<br/>REST + gRPC]

# &#x20;       ES\[EventService<br/>:5002<br/>REST]

# &#x20;       CS\[CommissionService<br/>:5003<br/>REST]

# &#x20;       WS\[WalletService<br/>:5004<br/>REST]

# &#x20;   end

# 

# &#x20;   subgraph "Инфраструктура"

# &#x20;       PG\[(PostgreSQL<br/>4 базы)]

# &#x20;       RMQ\[RabbitMQ<br/>Брокер сообщений]

# &#x20;       RD\[(Redis<br/>Кэш + Locks)]

# &#x20;   end

# 

# &#x20;   Client -->|REST| US

# &#x20;   Client -->|REST| ES

# &#x20;   Client -->|REST| CS

# &#x20;   Client -->|REST| WS

# 

# &#x20;   US -->|gRPC| CS

# &#x20;   ES -->|AMQP| RMQ

# &#x20;   CS -->|AMQP| RMQ

# &#x20;   WS -->|AMQP| RMQ

# 

# &#x20;   US --> PG

# &#x20;   ES --> PG

# &#x20;   CS --> PG

# &#x20;   WS --> PG

# 

# &#x20;   CS --> RD

# &#x20;   WS --> RD

# 

# &#x20;   style US fill:#4CAF50,color:#fff

# &#x20;   style ES fill:#2196F3,color:#fff

# &#x20;   style CS fill:#FF9800,color:#fff

# &#x20;   style WS fill:#9C27B0,color:#fff


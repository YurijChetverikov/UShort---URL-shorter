# UShort — High-Performance URL Shortener & Analytics System

[![License: MIT](https://img.shields.io/badge/License-GPL_3.0-blue.svg)](LICENSE)
[![Target Framework](https://img.shields.io/badge/.NET-9.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/ru-ru/download/dotnet/10.0)

**English** | [Русский](README.ru.md)

A lightweight, high-performance URL shortener built with ASP.NET Core 10 and Vue.js 3. The system not only shortens links and generates instant QR codes, but also tracks detailed redirect and creation analytics asynchronously using an in-memory channel pipeline and high-speed PostgreSQL binary bulk insertion.

---

## Try it out 

*Clickable*
[![Working site URL](/img/pic1.png)](https://ushort.cloud-ip.cc/index.html)

## 🚀 Key Features

* Instant Link Shortening & QR Generation: Convert long URLs into concise, readable links with automatically generated QR codes for seamless mobile access.
* O(1) Hash-Based Duplicate Detection: Prevents storing duplicate long URLs by indexing their SHA-512 hash in a dedicated hash lookup table, instantly returning the existing short link ID if found.
* Non-Blocking Analytics Pipeline: User access metrics are processed out-of-band using System.Threading.Channels, ensuring near-zero latency overhead for redirection endpoints.
* High-Throughput Batch Processing: Analytics events are flushed to PostgreSQL via high-performance COPY BINARY bulk streaming instead of traditional INSERT statements.
* IP Geo-Location Tracking: Captures user continent, country, city, precise coordinates, and accuracy radius using local MaxMind GeoLite2 City databases.
* Comprehensive Event Logging: Logs both link creation and redirect events—including failed attempts to access invalid or broken short links.

---

## 🛠️ Tech Stack & Dependencies

### Backend
* Framework: .NET 10 / ASP.NET Core
* PostgreSQL 18
* ORM & Data Access: [Dapper](https://github.com/DapperLib/Dapper) / [Dapper.AOT](https://github.com/DapperLib/Dapper) (High-performance micro-ORM with Native AOT compatibility)
* Database Driver: [Npgsql](https://github.com/npgsql/npgsql) (PostgreSQL provider with low-level binary copy support)
* Geo IP Resolution: [MaxMind.Db](https://github.com/maxmind/MaxMind-DB-Reader-dotnet) (GeoLite2-City.mmdb)

### Frontend
* Framework: Vue.js 3
* UI & Styling: Bootstrap 5

---

## 🚀 Performance

Measured locally on a multi-core machine with [`hey`](https://github.com/rakyll/hey) (`-c 50`, localhost, no TLS):

| Metric                | Value           |
|-----------------------|-----------------|
| Requests processed    | 1,000,000       |
| Total time            | 16.27 s         |
| Throughput            | **~61,400 RPS** |
| p50 latency           | 0.7 ms          |
| p99 latency           | 3.4 ms          |
| Max latency           | 130 ms          |
| Error rate (5xx)      | **0**           |

On the production-like VPS (1 vCPU / 512 MB RAM), the service handles ~782 RPS 
on short bursts; sustained load is limited by available RAM, not by application code.

The biggest single optimization was replacing per-request `INSERT`s with 
`COPY BINARY` batching. Before that change the same VPS showed p99 ≈ 1 s 
and multi-second stalls under load.

---

## 🏗️ Architecture & Performance Design

![Scheme](/img/scheme.jpg)

### 1. Asynchronous Event Pipeline (System.Threading.Channels)
When a user accesses a short URL or creates a new one, recording analytics synchronously would slow down the HTTP response. To maintain ultra-fast redirects, incoming event telemetry is published into a background channel and consumed asynchronously by a dedicated worker service.

### 2. High-Speed Bulk Persistence (COPY BINARY + Batching)
Instead of executing single INSERT queries per event:
* Events are buffered in memory and flushed either every 1 second or when the queue reaches 100 records.
* The buffer is written to PostgreSQL using the native NpgsqlBinaryImporter (COPY ... FROM STDIN (FORMAT BINARY)), bypassing standard SQL parsing overhead for maximum write throughput.

### 3. Fast Duplicate Link Check via SHA-512
Before persisting a new URL:
1. The system computes the SHA-512 hash of the target long URL.
2. A fast lookup is performed against a hash table mapping SHA-512 -> LinkID.
3. If a match exists, the system immediately returns the existing shortened URL without re-creating records.

---

## 📊 Analytics & Event Schema

Every system interaction records a standardized event row with the following telemetry:

| Column | Description |
| :--- | :--- |
| Event Type | 'R' for Redirect (click), 'C' for Link Creation |
| Link ID | Target link ID (-1 if the shortened link was invalid/broken) |
| User-Agent | Client device and browser user-agent header |
| Continent ID | MaxMind GeoLite2 Continent ID (if resolved) |
| Country ID | MaxMind GeoLite2 Country ID (if resolved) |
| City ID | MaxMind GeoLite2 City ID (if resolved) |
| Coordinates | Latitude & Longitude (if resolved) |
| Accuracy Radius | Geo-location accuracy radius |

---
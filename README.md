# Coworking Platform

Backend system for managing bookings across coworking spaces with different time zones and working schedules.

## Overview

This project explores backend architecture and concurrency handling in booking systems.

## Key Features

- Booking concurrency control: application-level coordination, serializable transactions, optimistic locking on PostgreSQL `xmin`
- Clean Architecture with CQRS (MediatR) and validation pipeline (FluentValidation, behaviors)
- Rate-limited REST API with centralized error handling (ProblemDetails, `Retry-After` on conflicts)
- Reliable messaging over a transactional outbox (MassTransit, RabbitMQ) with tiered retry and delayed redelivery
- Background processing using message consumers and a periodic hosted service
- PostgreSQL integration via EF Core with transaction handling and per-provider conflict detection
- Squidex Headless CMS client (not yet integrated)
- Unit and integration test suites

## Tech Stack

- .NET 10, ASP.NET Core
- EF Core (PostgreSQL)
- MassTransit (RabbitMQ)
- MediatR (CQRS)
- FluentValidation
- Polly
- MailKit
- Docker (API + PostgreSQL + RabbitMQ)

## Notes

This project is experimental and focuses on architectural patterns and concurrency strategies.  
In a production system, design decisions would be driven by specific business requirements and scale.

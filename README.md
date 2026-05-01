# Coworking Platform

Backend system for managing bookings across coworking spaces with different time zones and working schedules.

## Overview

This project explores backend architecture and concurrency handling in booking systems.

## Key Features

- Booking workflow with concurrency control using application-level coordination and database transactions (Range locks)
- Clean Architecture with CQRS (MediatR) and validation pipeline (FluentValidation, behaviors)
- Rate-limited REST API with centralized error handling (ProblemDetails)
- Background processing using Channels, parallel workers, and retry policies (Polly)
- PostgreSQL integration via EF Core with transaction handling and conflict detection
- Squidex Headless CMS integration (stage: unit testing)

## Tech Stack

- .NET 10, ASP.NET Core
- EF Core (PostgreSQL)
- MediatR (CQRS)
- FluentValidation
- Polly
- Docker (API + PostgreSQL)

## Notes

This project is experimental and focuses on architectural patterns and concurrency strategies.  
In a production system, design decisions would be driven by specific business requirements and scale.
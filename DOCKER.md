# Docker

## Production

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

## Full stack (app + infra)

```bash
docker compose up -d --build
docker compose down
```

## Infra only (local development)

```bash
docker compose -f docker-compose.infra.yml up -d
docker compose -f docker-compose.infra.yml down
```

## Logs

```bash
docker compose logs -f coworking.api
docker compose logs -f rabbitmq
docker compose logs -f db
```

## Reset database

```bash
docker compose down -v    # removes volumes (all data)
docker compose up -d
```

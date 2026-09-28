# Messaging: consumer split (parked)

Splitting consumers into a separate notification service — `AddMessagingConsumers` + a dedicated `MessagingDbContext`, plus a generic `AddMessaging<TDbContext>` — was parked on 2026-09-24: the scope is too large and there is no driver yet for running it as a second process.

**Why parked:** the real cost is not a second `DbContext`. `Coworking.Infrastructure` cannot be consumed partially — it bundles the email stack together with the repositories, and through that references `Coworking.Infrastructure.Persistence`. A notification service built today would end up with `AppDbContext` and full booking access anyway, which is exactly what the split is meant to avoid. `AddInfrastructure` also pulls in `BookingLockExpiryCleaner` with one call, and the email templates depend on Squidex.

**If revisited:** step 1, without changing deployment — extract the email stack into its own project that does not reference `Persistence`, together with the email model types currently under `Application/Features/Bookings/.../Notifications/Models/`. After that, splitting the deployment becomes a configuration change, not a refactor. The DB split leaned toward a shared database with role-based table ownership: the API keeps `outbox_*`, the notification service takes `inbox_state`. A generic `AddMessaging<TDbContext>` was rejected separately from this plan — it trades a compiler-checked guarantee for an unenforced convention, and only earns its place once a second real `DbContext` actually exists.

See [Coworking.Messaging/DependencyInjection.cs](../../Coworking/src/Coworking.Messaging/DependencyInjection.cs) for the current three entry points (`AddMessaging`, `AddMessagingPublishers`, `AddMessagingConsumers`) that this plan would build on.

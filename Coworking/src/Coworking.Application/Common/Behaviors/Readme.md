
TransactionBehavior




Application/
├── Common/
│   ├── Behaviors/           <-- TransactionBehavior, LoggingBehavior, ValidationBehavior
│   ├── Exceptions/          <-- BookingOverlapException
│   ├── Interfaces/          <-- IUnitOfWork, ITransactionalRequest
│   └── PipelineConfig/      <-- (Опционально) Pre/Post процессоры, если они общие
├── Bookings/
│   ├── Commands/
│   │   └── CreateBooking/
│   │       ├── CreateBookingCommand.cs
│   │       ├── CreateBookingCommandHandler.cs
│   │       └── CreateBookingValidator.cs  <-- (FluentValidation)
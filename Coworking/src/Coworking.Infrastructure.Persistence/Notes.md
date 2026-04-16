
migrations workflow:

cd src/Coworking.Infrastructure.Persistence
dotnet ef migrations add Init
dotnet ef database update
dotnet ef migrations remove






Other tips:


for factory
dotnet ef migrations add Init \
--project Coworking/src/Coworking.Infrastructure.Persistence


from the factory directory
dotnet ef migrations add Init

dotnet ef migrations add Init -s ../Coworking.API (without factory)


from slnx directory (without factory)
dotnet ef migrations add Init \
--project Coworking/src/Coworking.Infrastructure.Persistence \
--startup-project Coworking/src/Coworking.API


without factory
dotnet ef migrations add Init ^
-p Coworking/src/Coworking.Infrastructure.Persistence ^
-s Coworking/src/Coworking.API



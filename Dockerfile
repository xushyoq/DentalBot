FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DentalBot.Bot/DentalBot.Bot.csproj DentalBot.Bot/
COPY DentalBot.Infrastructure/DentalBot.Infrastructure.csproj DentalBot.Infrastructure/
COPY DentalBot.Application/DentalBot.Application.csproj DentalBot.Application/
COPY DentalBot/DentalBot.Domain.csproj DentalBot/

RUN dotnet restore DentalBot.Bot/DentalBot.Bot.csproj

COPY . .

RUN dotnet publish DentalBot.Bot/DentalBot.Bot.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DentalBot.Bot.dll"]

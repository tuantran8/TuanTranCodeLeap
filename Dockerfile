# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY TuanTranCodeLeap.sln ./
COPY TuanTranCodeLeap.Domain/TuanTranCodeLeap.Domain.csproj TuanTranCodeLeap.Domain/
COPY TuanTranCodeLeap.Application/TuanTranCodeLeap.Application.csproj TuanTranCodeLeap.Application/
COPY TuanTranCodeLeap.Infrastructure/TuanTranCodeLeap.Infrastructure.csproj TuanTranCodeLeap.Infrastructure/
COPY TuanTranCodeLeap.API/TuanTranCodeLeap.API.csproj TuanTranCodeLeap.API/

RUN dotnet restore TuanTranCodeLeap.API/TuanTranCodeLeap.API.csproj

# Copy everything and build
COPY . .
RUN dotnet build TuanTranCodeLeap.API/TuanTranCodeLeap.API.csproj -c Release --no-restore
RUN dotnet publish TuanTranCodeLeap.API/TuanTranCodeLeap.API.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "TuanTranCodeLeap.API.dll"]

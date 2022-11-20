#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DAO_CasperChainService/DAO_CasperChainService.csproj", "DAO_CasperChainService/"]
COPY ["Helpers/Helpers.csproj", "Helpers/"]
RUN dotnet restore "DAO_CasperChainService/DAO_CasperChainService.csproj"
COPY . .
WORKDIR "/src/DAO_CasperChainService"
RUN dotnet build "DAO_CasperChainService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DAO_CasperChainService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DAO_CasperChainService.dll"]

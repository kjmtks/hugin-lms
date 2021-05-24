#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update -yq &&\
    apt-get install -yq git binutils debootstrap


FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Hugin/Hugin.csproj", "/src/Hugin/"]
COPY ["Hugin/package.json", "/src/Hugin/"]
COPY ["Hugin/package-lock.json", "/src/Hugin/"]
RUN dotnet restore "Hugin/Hugin.csproj"
COPY . .
WORKDIR "/src/Hugin"
RUN dotnet tool install --global dotnet-ef --version 5.0
RUN apt-get update -yq &&\
    apt-get install curl gnupg -yq 
RUN dotnet build "Hugin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hugin.csproj" -c Release -o /app/publish

#FROM base AS final
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["/bin/sh", "-c", "dotnet Hugin.dll"]

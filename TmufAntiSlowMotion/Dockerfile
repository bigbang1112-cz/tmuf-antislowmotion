FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["TmufAntiSlowMotion/TmufAntiSlowMotion.csproj", "TmufAntiSlowMotion/"]
COPY ["TmufAntiSlowMotionLib/TmufAntiSlowMotionLib.csproj", "TmufAntiSlowMotionLib/"]
RUN dotnet restore "TmufAntiSlowMotion/TmufAntiSlowMotion.csproj"
COPY . .
WORKDIR "/src/TmufAntiSlowMotion"
RUN dotnet build "TmufAntiSlowMotion.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TmufAntiSlowMotion.csproj" -c Release -o /app/publish -r linux-musl-x64 -p:PublishReadyToRun=true --self-contained

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./TmufAntiSlowMotion"]

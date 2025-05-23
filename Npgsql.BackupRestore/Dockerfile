﻿FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends \
      curl \
      wget \
      gnupg \
      apt-transport-https \
      ca-certificates \
      software-properties-common \
    && apt-get clean
# Add apt repository for PostgreSQL
RUN echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | apt-key add -
RUN apt-get update && apt-get install -y --no-install-recommends \
      postgresql-13  \
      postgresql-14  \
      postgresql-15  \
      postgresql-16 \
    && apt-get clean

WORKDIR /app

COPY Npgsql.BackupRestore/Npgsql.BackupRestore.csproj Npgsql.BackupRestore/
COPY Npgsql.BackupRestore.Tests/Npgsql.BackupRestore.Tests.csproj Npgsql.BackupRestore.Tests/
RUN dotnet restore Npgsql.BackupRestore.Tests/Npgsql.BackupRestore.Tests.csproj

COPY . .
ENTRYPOINT [ "dotnet", "test", "Npgsql.BackupRestore.Tests/Npgsql.BackupRestore.Tests.csproj", "--logger=\"console;verbosity=detailed\"" ]
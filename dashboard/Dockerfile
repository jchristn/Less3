FROM node:22-slim
WORKDIR /app
RUN apt-get update && apt-get install -y \
    # Networking tools
    iputils-ping \
    traceroute \
    net-tools \
    curl \
    wget \
    dnsutils \
    iproute2 \
    netcat-openbsd \
    telnet \
    tcpdump \
    # Editors and utilities
    vim \
    less \
    file \
    jq \
    # Process tools
    procps \
    htop \
    # Misc
    xsel \
    && rm -rf /var/lib/apt/lists/*
COPY . .
RUN npm install
ARG LESS3_SERVER_URL=http://localhost:3000
ENV LESS3_SERVER_URL=$LESS3_SERVER_URL
RUN npm run build
RUN chmod +x entrypoint.sh
EXPOSE 3000
ENTRYPOINT ["./entrypoint.sh"]

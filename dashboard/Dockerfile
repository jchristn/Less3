FROM node:22-slim
WORKDIR /app
RUN apt-get update && apt-get install -y iputils-ping traceroute net-tools curl wget dnsutils iproute2 file vim procps xsel && rm -rf /var/lib/apt/lists/*
COPY . .
RUN npm install
RUN npm run build
EXPOSE 3000
CMD [ "npm", "run", "start" ]

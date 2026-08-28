# Build stage
FROM node:26-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install

COPY . .
RUN npm run docs:build

# Production stage
FROM reg.mini.dev/nginx
COPY --from=build /app/.vitepress/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
# Base image sets ENTRYPOINT to nginx; pass flags only (not "nginx" again).
CMD ["-g", "daemon off;"]

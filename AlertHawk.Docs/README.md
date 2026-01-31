# AlertHawk Documentation

This folder contains the project documentation built with [VitePress](https://vitepress.dev/).

## Structure

- **`index.md`** — Home page
- **`intro.md`** — Introduction and architecture overview
- **`authentication/`** — AlertHawk.Authentication
- **`metrics-agent/`** — Metrics Agent
- **`metrics-api/`** — Metrics API
- **`monitoring/`** — AlertHawk.Monitoring
- **`notification/`** — AlertHawk.Notification
- **`.vitepress/config.js`** — VitePress and theme config

## Commands

```bash
# Install dependencies
npm install

# Start dev server (with hot reload)
npm run docs:dev

# Build for production
npm run docs:build

# Preview production build
npm run docs:preview
```

## Docker

Build and run the docs as a container:

```bash
docker build -t alerthawk-docs .
docker run -p 8080:80 alerthawk-docs
```

Then open http://localhost:8080.

## Adding Content

- Edit the `.md` files in each section.
- Add new pages under the corresponding folder and link them in `.vitepress/config.js` under `themeConfig.sidebar` and optionally `themeConfig.nav`.
- Static assets (images, etc.) go in the `public/` folder and are served at `/`.

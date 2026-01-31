import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'AlertHawk',
  description: 'Self-hosted monitoring solution - Documentation',
  base: '/',
  ignoreDeadLinks: true,
  themeConfig: {
    nav: [
      { text: 'Introduction', link: '/intro' },
      { text: 'Authentication', link: '/authentication/' },
      { text: 'Metrics Agent', link: '/metrics-agent/' },
      { text: 'Metrics API', link: '/metrics-api/' },
      { text: 'Monitoring', link: '/monitoring/' },
      { text: 'Notification', link: '/notification/' },
      { text: 'Helm Chart', link: '/helm/' },
      { text: 'Agents Chart', link: '/agents-chart/' },
    ],
    sidebar: [
      {
        text: 'Getting Started',
        collapsed: false,
        items: [
          { text: 'Introduction', link: '/intro' },
        ],
      },
      {
        text: 'Authentication',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/authentication/' },
          { text: 'Environment Variables', link: '/authentication/#environment-variables' },
          { text: 'API Controllers', link: '/authentication/#api-controllers' },
          { text: 'Helm Chart', link: '/authentication/#helm-chart-reference' },
        ],
      },
      {
        text: 'Metrics Agent',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/metrics-agent/' },
          { text: 'Environment Variables', link: '/metrics-agent/#environment-variables' },
          { text: 'What It Collects', link: '/metrics-agent/#what-it-collects' },
          { text: 'Data Sent to Metrics API', link: '/metrics-agent/#data-sent-to-the-metrics-api' },
          { text: 'Deployment', link: '/metrics-agent/#deployment' },
          { text: 'Troubleshooting', link: '/metrics-agent/#troubleshooting' },
        ],
      },
      {
        text: 'Metrics API',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/metrics-api/' },
          { text: 'Environment Variables', link: '/metrics-api/#environment-variables' },
          { text: 'API Controllers', link: '/metrics-api/#api-controllers' },
          { text: 'Data Flow', link: '/metrics-api/#data-flow' },
          { text: 'Helm Chart', link: '/metrics-api/#helm-chart-reference' },
        ],
      },
      {
        text: 'Monitoring',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/monitoring/' },
          { text: 'Environment Variables', link: '/monitoring/#environment-variables' },
          { text: 'API Controllers', link: '/monitoring/#api-controllers' },
          { text: 'Helm Chart', link: '/monitoring/#helm-chart-reference' },
        ],
      },
      {
        text: 'Notification',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/notification/' },
          { text: 'Environment Variables', link: '/notification/#environment-variables' },
          { text: 'API Controllers', link: '/notification/#api-controllers' },
          { text: 'Supported Channels', link: '/notification/#supported-channels' },
          { text: 'Helm Chart', link: '/notification/#helm-chart-reference' },
        ],
      },
      {
        text: 'Helm Chart',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/helm/' },
          { text: 'Installation', link: '/helm/installation' },
          { text: 'Configuration', link: '/helm/configuration' },
          { text: 'Environment Variables', link: '/helm/environment-variables' },
          { text: 'Agents Chart (Metrics Agent)', link: '/agents-chart/' },
        ],
      },
      {
        text: 'Agents Chart',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/agents-chart/' },
          { text: 'Installation', link: '/agents-chart/installation' },
          { text: 'Configuration', link: '/agents-chart/configuration' },
          { text: 'Environment Variables', link: '/agents-chart/environment-variables' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/thiagoloureiro/AlertHawk' },
    ],
    footer: {
      message: 'AlertHawk - Self-hosted monitoring solution.',
      copyright: 'Copyright © AlertHawk Contributors',
    },
  },
})

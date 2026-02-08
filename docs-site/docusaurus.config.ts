import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'MoonBuggy',
  tagline: 'Compile-time i18n for .NET',
  favicon: 'img/moonbuggy-logo.svg',

  future: {
    v4: true,
  },

  url: 'https://intelligenthack.github.io',
  baseUrl: '/moonbuggy/',

  organizationName: 'intelligenthack',
  projectName: 'moonbuggy',
  trailingSlash: false,

  onBrokenLinks: 'throw',

  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/intelligenthack/moonbuggy/tree/main/docs-site/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/moonbuggy-logo.svg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'MoonBuggy',
      logo: {
        alt: 'MoonBuggy Logo',
        src: 'img/moonbuggy-logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://www.nuget.org/packages/intelligenthack.MoonBuggy',
          label: 'NuGet',
          position: 'right',
        },
        {
          href: 'https://github.com/intelligenthack/moonbuggy',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/getting-started',
            },
            {
              label: 'Syntax Reference',
              to: '/docs/guides/syntax-reference',
            },
            {
              label: 'CLI Reference',
              to: '/docs/api/cli-reference',
            },
          ],
        },
        {
          title: 'Resources',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/intelligenthack/moonbuggy',
            },
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/packages/intelligenthack.MoonBuggy',
            },
          ],
        },
        {
          title: 'Legal',
          items: [
            {
              label: 'MIT License',
              href: 'https://github.com/intelligenthack/moonbuggy/blob/main/LICENSE',
            },
          ],
        },
      ],
      copyright: `Copyright \u00a9 ${new Date().getFullYear()} IntelligentHack. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'json', 'xml-doc', 'yaml', 'gettext'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;

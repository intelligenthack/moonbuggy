import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    'getting-started',
    {
      type: 'category',
      label: 'Guides',
      collapsed: false,
      items: [
        'guides/syntax-reference',
        'guides/pluralization',
        'guides/markdown-translations',
        'guides/pseudolocalization',
        'guides/lingui-coexistence',
        'guides/testing',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      collapsed: false,
      items: [
        'api/cli-reference',
        'api/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Resources',
      collapsed: false,
      items: [
        'resources/benchmarks',
      ],
    },
  ],
};

export default sidebars;

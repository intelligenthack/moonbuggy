import React from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import CodeBlock from '@theme/CodeBlock';
import {useColorMode} from '@docusaurus/theme-common';

function HeroWordmark() {
  const {colorMode} = useColorMode();
  const src = colorMode === 'dark'
    ? '/moonbuggy/img/moonbuggy-wordmark-light.svg'
    : '/moonbuggy/img/moonbuggy-wordmark.svg';
  return <img src={src} alt="MoonBuggy" className="hero-wordmark" />;
}

function Hero() {
  return (
    <div className="hero-section">
      <div className="container">
        <HeroWordmark />
        <h1 className="hero__title">Compile-time i18n for .NET</h1>
        <p className="hero__subtitle">
          Zero-allocation translations via source generators.
          ICU MessageFormat with CLDR plural rules.
          Industry-standard PO files.
        </p>
        <div className="hero-buttons">
          <Link className="button button--primary button--lg" to="/docs/getting-started">
            Get Started
          </Link>
          <Link
            className="button button--secondary button--lg"
            href="https://github.com/intelligenthack/moonbuggy">
            View on GitHub
          </Link>
        </div>
      </div>
    </div>
  );
}

const features = [
  {
    title: 'Zero Allocations',
    description:
      'The source generator emits direct TextWriter.Write chains. No dictionary lookups, no string formatting, no per-request allocations.',
  },
  {
    title: 'ICU MessageFormat',
    description:
      'Full CLDR plural rules for all Unicode locales, baked into the binary as inline arithmetic. No ICU runtime library needed.',
  },
  {
    title: 'PO File Format',
    description:
      'Industry-standard format supported by Crowdin, Weblate, Poedit, and every major translation management system.',
  },
  {
    title: 'Compile-Time Safety',
    description:
      'Diagnostics MB0001\u2013MB0009 catch missing variables, malformed syntax, and type errors directly in your IDE.',
  },
  {
    title: 'Lingui.js Compatible',
    description:
      'Share PO files between .NET and JavaScript. Both extractors use ICU MessageFormat as the msgid key.',
  },
  {
    title: 'Markdown Support',
    description:
      '_m() converts markdown to HTML with reorderable numbered placeholders. Translators never touch raw HTML.',
  },
];

function Features() {
  return (
    <div className="features-section">
      <div className="container">
        <div className="features-grid">
          {features.map((feature, idx) => (
            <div key={idx} className="feature-card">
              <h3>{feature.title}</h3>
              <p>{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

const razorSource = `@* _ViewImports.cshtml *@
@using static MoonBuggy.Translate

@* In any view *@
<h1>@_t("Welcome to $name$!", new { name = Model.SiteName })</h1>
<p>@_t("You have $#x# item|#x# items$", new { x = Model.Count })</p>
<p>@_m("Click **[here]($url$)** to continue", new { url })</p>`;

const generatedCode = `// Emitted by the source generator (conceptual)
switch (lcid) {
    case 10: // Spanish
        writer.Write("Tienes ");
        if (x == 1) { writer.Write("1 art\u00edculo"); }
        else { writer.Write(x); writer.Write(" art\u00edculos"); }
        break;
    default: // English (source locale)
        writer.Write("You have ");
        if (x == 1) { writer.Write("1 item"); }
        else { writer.Write(x); writer.Write(" items"); }
        break;
}`;

function CodeExample() {
  return (
    <div className="code-section">
      <div className="container">
        <h2>How it works</h2>
        <div className="code-columns">
          <div className="code-column">
            <h3>You write Razor views</h3>
            <CodeBlock language="html">{razorSource}</CodeBlock>
          </div>
          <div className="code-column">
            <h3>The compiler generates direct writes</h3>
            <CodeBlock language="csharp">{generatedCode}</CodeBlock>
          </div>
        </div>
      </div>
    </div>
  );
}

const installCommands = `dotnet add package intelligenthack.MoonBuggy
dotnet add package intelligenthack.MoonBuggy.SourceGenerator
dotnet tool install intelligenthack.MoonBuggy.Cli`;

function Install() {
  return (
    <div className="install-section">
      <div className="container">
        <h2>Quick install</h2>
        <CodeBlock language="bash">{installCommands}</CodeBlock>
      </div>
    </div>
  );
}

export default function Home(): React.JSX.Element {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout title="Home" description={siteConfig.tagline}>
      <main>
        <Hero />
        <Features />
        <CodeExample />
        <Install />
      </main>
    </Layout>
  );
}

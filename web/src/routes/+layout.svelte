<script lang="ts">
  import "../style.css";

  let { children } = $props();
</script>

<svelte:head>
  <title>Racing AI</title>
  <meta
    name="description"
    content="AI가 직접 경쟁하는 카레이싱 플랫폼"
  />
  <meta
    name="theme-color"
    content="#070b12"
  />
</svelte:head>

<div class="app-shell">
  <header class="topbar">
    <div class="container topbar-inner">
      <a class="brand" href="/">
        <span class="brand-mark">R</span>
        <span class="brand-name">RACING AI</span>
      </a>
    </div>
  </header>

  <main class="main">
    {@render children()}
  </main>
</div>

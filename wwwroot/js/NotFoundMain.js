const { createApp, ref, watch, nextTick } = Vue;

const App = {
  name: 'App',
  components: { TextField, SubmitButton, ResultContainer },

  setup() {
    // ---- State ---------------------------------------------------------
    const inputText   = ref('');
    const loading     = ref(false);

    // ---- Theme ---------------------------------------------------------
    const savedTheme = localStorage.getItem('theme');
    const theme = ref(savedTheme === 'dark' ? 'dark' : 'light');

    function applyTheme(t) {
      document.documentElement.setAttribute('data-theme', t);
    }
    applyTheme(theme.value);

    watch(theme, (t) => {
      applyTheme(t);
      localStorage.setItem('theme', t);
    });

    function toggleTheme() {
      theme.value = theme.value === 'light' ? 'dark' : 'light';
    }

    // ---- Return to main page --------------------------------------------------------
    function onReturnToMainPage() {
        window.location = 'index.html';
    }

    return {
      inputText,
      loading,
      theme,
      onReturnToMainPage,
      toggleTheme,
    };
  },

  template: `
    <div class="app-wrapper">
      <!-- Theme toggle, top-right -->
      <button
        class="theme-toggle"
        type="button"
        :title="theme === 'light' ? 'Тёмная тема' : 'Светлая тема'"
        :aria-label="theme === 'light' ? 'Включить тёмную тему' : 'Включить светлую тему'"
        @click="toggleTheme"
      >
        <span v-if="theme === 'light'">🌙</span>
        <span v-else>☀️</span>
      </button>

      <main class="app-main">
        <h1 class="app-title">Сократить ссылку</h1>

        <h2 class="app-title">Ссылка не найдена</h2>

        <div class="button-block">
          <SubmitButton label='На главную' :loading="loading" @click="onReturnToMainPage" />
        </div>
      </main>
    </div>
  `,
};

createApp(App).mount('#app');
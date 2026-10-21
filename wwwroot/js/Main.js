const { createApp, ref, watch, nextTick } = Vue;



const App = {
  name: 'App',
  components: { TextField, SubmitButton, ResultContainer },

  setup() {
    // ---- State ---------------------------------------------------------
    const inputText   = ref('');
    const loading     = ref(false);
    const shortUrl    = ref('');
    const showResult  = ref(false);
    const textFieldRef = ref(null);

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

    // ---- Input handling -----------------------------------------------
    function onInputChange(value) {
      inputText.value = value;
      // User edited the input — the previously generated result is stale
      if (showResult.value) {
        showResult.value = false;
      }
    }

    // ---- Submit --------------------------------------------------------
    async function onSubmit() {
      // Hide any previous result before starting a new request
      showResult.value = false;

      // Reset any previous error
      textFieldRef.value?.clearError();

      // Basic client-side guard
      if (!inputText.value.trim()) {
        textFieldRef.value?.showError('Введите ссылку');
        return;
      }

      loading.value = true;

    try {
        // === Replace with your real endpoint ===
        const response = await fetch(`/api/createLink?link=${inputText.value.trim()}`, {
          method: 'POST'
        });

        // ----- Server returned the shortened link ----
        if (response.ok) {
          let value = '';
          value = (await response.text()).trim();
          if (!value) {
            textFieldRef.value?.showError('Сервер вернул пустой ответ');
            return;
          }

          shortUrl.value = new URL(`/r/${base58.encode(Number(value))}`, location.origin).href;
          // Let the DOM update first, then reveal the panel so the
          // transition plays from a real height.
        //   await nextTick();
          showResult.value = true;
          return;
        }

        // ----- Bad Request ----
        if (response.status === 400) {
          const code = (await response.text()).trim();
          let message;
          switch (code) {
            case `Requested parameter "link" is not valid URL` || `Requested parameter "link" is not valid URL`:
                message = 'Ссылка имеет некорректный вид';
                break;
            case `The length of parameter "link" exceeds the limit`:
                message = 'Ссыка слишком длинная';
                break;
            default:
                message = `Некорректный запрос: ${message}`;
          }
          textFieldRef.value?.showError(message);
          return;
        }

        // ----- 500 Internal Server Error ----
        if (response.status === 500) {
          const code = (await response.text()).trim();
          let message;
          switch (code) {
            case 'db_error':         message = 'Ошибка базы данных. Попробуйте позже'; break;
            case 'shortener_error':  message = 'Сервис сокращения недоступен';          break;
            case 'timeout':          message = 'Превышено время ожидания';              break;
            default:                 message = code || 'Внутренняя ошибка сервера';
          }
          textFieldRef.value?.showError(message);
          return;
        }

        // ----- Any other status ----
        textFieldRef.value?.showError(`Неизвестный ответ сервера (${response.status})`);
      } catch (err) {
        textFieldRef.value?.showError('Ошибка сети: ' + (err?.message || 'неизвестно'));
      } finally {
        loading.value = false;
      }
    }

    return {
      inputText,
      loading,
      shortUrl,
      showResult,
      textFieldRef,
      theme,
      onInputChange,
      onSubmit,
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

        <div class="input-block">
          <TextField
            ref="textFieldRef"
            :model-value="inputText"
            placeholder="Вставьте длинную ссылку…"
            @update:model-value="onInputChange"
          />
        </div>

        <div class="button-block">
          <SubmitButton :loading="loading" @click="onSubmit" />
        </div>

        <div class="result-block">
          <ResultContainer :url="shortUrl" :visible="showResult" />
        </div>
      </main>
    </div>
  `,
};

createApp(App).mount('#app');
/* Input field with a hidden error banner above it.
   The parent can call `showError(msg)` via a template ref. */
const TextField = {
  name: 'TextField',
  props: {
    modelValue: { type: String, default: '' },
    placeholder: { type: String, default: '' },
  },
  emits: ['update:modelValue'],

  setup(props, { emit, expose }) {
    const error = Vue.ref('');

    // Public API for the parent
    function showError(message) {
      error.value = message || '';
    }
    function clearError() {
      error.value = '';
    }

    function onInput(event) {
      emit('update:modelValue', event.target.value);
      // Any typing dismisses the current error
      if (error.value) error.value = '';
    }

    // Expose only the imperative helpers to the parent via template ref
    expose({ showError, clearError });

    return { error, onInput };
  },

  template: `
    <div class="text-field">
      <!-- Error banner: sits above the input, animates in/out -->
      <transition name="fade">
        <div v-if="error" class="error-banner" role="alert">
          {{ error }}
        </div>
      </transition>

      <input
        type="text"
        class="form-control text-field__input"
        :value="modelValue"
        :placeholder="placeholder"
        autocomplete="off"
        autocapitalize="off"
        spellcheck="false"
        @input="onInput"
      />
    </div>
  `,
};
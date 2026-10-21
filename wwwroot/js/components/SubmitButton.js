/* Submit button that swaps its text for a spinner while `loading` is true. */
const SubmitButton = {
  name: 'SubmitButton',
  props: {
    loading: { type: Boolean, default: false },
    label:   { type: String, default: 'Сократить' },
  },
  emits: ['click'],

  template: `
    <button
      type="button"
      class="btn submit-btn"
      :disabled="loading"
      :aria-busy="loading"
      @click="$emit('click')"
    >
      <span v-if="!loading">{{ label }}</span>
      <span
        v-else
        class="spinner-border spinner-border-sm"
        role="status"
        aria-hidden="true"
      ></span>
    </button>
  `,
};
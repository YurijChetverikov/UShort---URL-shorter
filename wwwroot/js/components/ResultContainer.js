/* Panel with the QR code and the shortened (clickable) link.
   Hidden with a smooth animation when `visible` goes false. */

const { ref, watch, onMounted } = Vue;
const QrcodeCanvas = QrcodeVue.QrcodeCanvas;

const ResultContainer = {
    name: 'ResultContainer',
    props: {
      url:     { type: String, default: '' },
      visible: { type: Boolean, default: false },
    },

    components: {
      QrcodeCanvas,
    },

    setup() {
      // Read the QR colors from colors.css so they stay centralized.
      // (QR codes must stay dark-on-light, so these are intentionally
      //  the same in both themes — see the note below.)
      const style = getComputedStyle(document.documentElement);
      const qrBg  = style.getPropertyValue('--color-qr-bg').trim()  || '#ffffff';
      const qrFg  = style.getPropertyValue('--color-qr-fg').trim()  || '#1e2a3b';

      return { qrBg, qrFg };
    },

    template: `
      <transition name="slide-fade">
        <div v-if="visible && url" class="result-container">
          <div class="result-container__qr">
            <!-- qrcode.vue reacts to :value and redraws automatically -->
            <qrcode-canvas
              :value="url"
              :size="200"
              level="H"
              :background="qrBg"
              :foreground="qrFg"
            />
          </div>

          <a
            :href="url"
            target="_blank"
            rel="noopener noreferrer"
            class="result-container__link"
          >
            {{ url }}
          </a>
        </div>
      </transition>
    `,
};
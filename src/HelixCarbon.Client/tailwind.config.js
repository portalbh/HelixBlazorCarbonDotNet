/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.{razor,html,cs}',
    '../../lib/CarbonBlazor/**/*.{razor,cs}'
  ],
  theme: {
    extend: {}
  },
  plugins: []
};

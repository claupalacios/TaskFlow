import { Provider } from '@angular/core';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';

/** Material components finish opening and closing at once, without waiting for CSS animations. */
export const noMaterialAnimations: Provider = {
  provide: MATERIAL_ANIMATIONS,
  useValue: { animationsDisabled: true },
};

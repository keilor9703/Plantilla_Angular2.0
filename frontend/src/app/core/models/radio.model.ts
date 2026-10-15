/** Configuración de una emisora consumida por el reproductor global. */
export interface RadioStation {
  id: string;
  name: string;
  streamUrl: string;
  logoUrl: string | null;
}

export type RadioPlaybackState = 'idle' | 'loading' | 'ready' | 'playing' | 'paused' | 'offline';

export type RadioConnectionState = 'checking' | 'online' | 'offline';

/** Estado breve visible y detalle técnico presentado en el tooltip. */
export interface RadioStatusView {
  state: RadioConnectionState;
  label: 'Verificando' | 'Online' | 'Offline';
  tooltip: string;
}

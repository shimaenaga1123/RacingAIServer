import { defineEnvVars } from '@sveltejs/kit/env';
import * as v from 'valibot';

export const variables = defineEnvVars({
  API_BASE: {
    public: true,
    schema: v.optional(v.pipe(v.string(), v.url()), "https://localhost:5164/api/v1")
  },
  SIGNALR_URL: {
    public: true,
    schema: v.optional(v.pipe(v.string(), v.url()), "https://localhost:5164/hubs/race")
  },
});

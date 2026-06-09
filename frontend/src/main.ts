import { bootstrapApplication } from '@angular/platform-browser';
import { isDevMode } from '@angular/core';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app';

if (!isDevMode()) {
  console.log  = () => {};
  console.warn = () => {};
  console.error = () => {};
  console.debug = () => {};
  console.info  = () => {};
}

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
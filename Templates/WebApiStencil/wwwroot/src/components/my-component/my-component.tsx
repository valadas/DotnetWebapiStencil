import { Component, Host, Prop, State, h } from '@stencil/core';
import { format } from '../../utils/utils';
import { WeatherForecast, WeatherForecastClient } from '../../services/api-clients/WeatherForecastClient';

@Component({
  tag: 'my-component',
  styleUrl: 'my-component.scss',
  shadow: true,
})
export class MyComponent {
  /**
   * The first name
   */
  @Prop() first: string;

  /**
   * The middle name
   */
  @Prop() middle: string;

  /**
   * The last name
   */
  @Prop() last: string;

  @State() forecasts: WeatherForecast[] = [];
  
  private apiClient: WeatherForecastClient;

  constructor() {
    this.apiClient = new WeatherForecastClient();
  }

  private getText(): string {
    return format(this.first, this.middle, this.last);
  }

  private getWeather(): void {
    this.apiClient.get().then((response) => {
      this.forecasts = response;
    });
  }

  render() {
    return <Host>
      <div class="surface">
        <p>Hello, World! I'm {this.getText()}</p>
        <button onClick={() => this.getWeather()}>Get Weather Forecast</button>
      </div>
      {this.forecasts &&
        <div class="forecasts">
          {this.forecasts.map(forecast =>
            <div class="forecast">
              <p>{forecast.date.toLocaleDateString()}</p>
              <p>{forecast.summary}</p>
              <p>{forecast.temperatureC} °C</p>
            </div>
          )}
        </div>
      }
    </Host>
  }
}

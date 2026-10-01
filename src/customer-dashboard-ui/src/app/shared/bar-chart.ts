import { afterNextRender, Component, effect, ElementRef, input, OnDestroy, viewChild } from '@angular/core';
import { init, use, type EChartsType } from 'echarts/core';
import { BarChart as EchartsBar } from 'echarts/charts';
import { GridComponent, LegendComponent, TooltipComponent, AriaComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';

use([EchartsBar, GridComponent, LegendComponent, TooltipComponent, AriaComponent, CanvasRenderer]);

@Component({
  selector: 'app-bar-chart',
  template: '<div #canvas class="chart" role="img" [attr.aria-label]="label()"></div>',
  styles: '.chart { width: 100%; height: 310px; }',
})
export class BarChartPanel implements OnDestroy {
  readonly categories = input.required<string[]>();
  readonly series = input.required<{ name: string; values: number[] }[]>();
  readonly unit = input('');
  readonly label = input('Chart. Exact figures are in the table.');
  private readonly canvas = viewChild.required<ElementRef<HTMLDivElement>>('canvas');
  private chart?: EChartsType;
  private observer?: ResizeObserver;

  constructor() {
    afterNextRender(() => {
      this.chart = init(this.canvas().nativeElement);
      this.observer = new ResizeObserver(() => this.chart?.resize());
      this.observer.observe(this.canvas().nativeElement);
      this.render();
    });
    effect(() => { this.categories(); this.series(); this.unit(); this.render(); });
  }

  private render() {
    if (!this.chart) return;
    const colors = ['#16796d', '#adc7de', '#d7b56d', '#8aa2b2'];
    this.chart.setOption({
      animation: !window.matchMedia('(prefers-reduced-motion: reduce)').matches,
      aria: { enabled: true },
      color: colors,
      tooltip: { trigger: 'axis' },
      legend: { bottom: 0, icon: 'roundRect' },
      grid: { left: 72, right: 16, top: 24, bottom: 64 },
      xAxis: { type: 'category', data: this.categories(), axisTick: { show: false }, name: 'Period' },
      yAxis: { type: 'value', name: this.unit(), splitLine: { lineStyle: { color: '#edf0f2' } } },
      series: this.series().map(item => ({ name: item.name, type: 'bar', data: item.values, barMaxWidth: 28, itemStyle: { borderRadius: [5, 5, 0, 0] } })),
    });
  }

  ngOnDestroy() { this.observer?.disconnect(); this.chart?.dispose(); }
}

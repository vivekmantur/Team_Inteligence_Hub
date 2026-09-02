import { useEffect, useRef } from "react";
import * as d3 from "d3";

interface SparklineProps {
  data: number[];
  color?: string;
  height?: number;
}

export function Sparkline({ data, color = "#6366f1", height = 40 }: SparklineProps) {
  const ref = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!ref.current) return;
    const svg = d3.select(ref.current);
    svg.selectAll("*").remove();
    const width = ref.current.clientWidth || 200;
    const h = height;
    const x = d3.scaleLinear().domain([0, data.length - 1]).range([2, width - 2]);
    const y = d3.scaleLinear().domain([d3.min(data)! * 0.95, d3.max(data)! * 1.05]).range([h - 4, 4]);

    const gradId = `spark-grad-${Math.random().toString(36).slice(2, 8)}`;
    const defs = svg.append("defs");
    const grad = defs
      .append("linearGradient")
      .attr("id", gradId)
      .attr("x1", "0")
      .attr("x2", "0")
      .attr("y1", "0")
      .attr("y2", "1");
    grad.append("stop").attr("offset", "0%").attr("stop-color", color).attr("stop-opacity", 0.4);
    grad.append("stop").attr("offset", "100%").attr("stop-color", color).attr("stop-opacity", 0);

    const area = d3
      .area<number>()
      .x((_, i) => x(i))
      .y0(h)
      .y1((d) => y(d))
      .curve(d3.curveMonotoneX);

    const line = d3
      .line<number>()
      .x((_, i) => x(i))
      .y((d) => y(d))
      .curve(d3.curveMonotoneX);

    svg
      .attr("viewBox", `0 0 ${width} ${h}`)
      .attr("width", "100%")
      .attr("height", h);

    svg.append("path").datum(data).attr("d", area).attr("fill", `url(#${gradId})`);
    svg
      .append("path")
      .datum(data)
      .attr("d", line)
      .attr("fill", "none")
      .attr("stroke", color)
      .attr("stroke-width", 2)
      .attr("stroke-linecap", "round");
  }, [data, color, height]);

  return <svg ref={ref} className="w-full block" />;
}

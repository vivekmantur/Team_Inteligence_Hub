import { useEffect, useRef } from "react";
import * as d3 from "d3";

interface BarChartProps {
  data: { label: string; value: number; color?: string }[];
  height?: number;
  format?: (n: number) => string;
}

export function BarChart({ data, height = 240, format }: BarChartProps) {
  const ref = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!ref.current) return;
    const svg = d3.select(ref.current);
    svg.selectAll("*").remove();
    const width = ref.current.clientWidth || 500;
    const margin = { top: 12, right: 12, bottom: 30, left: 40 };
    const iw = width - margin.left - margin.right;
    const ih = height - margin.top - margin.bottom;

    const x = d3.scaleBand().domain(data.map((d) => d.label)).range([0, iw]).padding(0.35);
    const y = d3.scaleLinear().domain([0, d3.max(data, (d) => d.value)! * 1.15]).range([ih, 0]);

    svg.attr("viewBox", `0 0 ${width} ${height}`).attr("width", "100%").attr("height", height);
    const g = svg.append("g").attr("transform", `translate(${margin.left},${margin.top})`);

    g.append("g")
      .call(d3.axisLeft(y).ticks(4).tickSize(-iw).tickFormat(() => "") as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("line").attr("stroke", "currentColor").attr("opacity", 0.08));

    g.append("g")
      .attr("transform", `translate(0,${ih})`)
      .call(d3.axisBottom(x) as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("text").attr("fill", "currentColor").attr("font-size", 10).attr("opacity", 0.7));

    g.append("g")
      .call(d3.axisLeft(y).ticks(4).tickFormat((d) => (format ? format(d as number) : d3.format(".2s")(d as number))) as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("line").remove())
      .call((sel) => sel.selectAll("text").attr("fill", "currentColor").attr("font-size", 10).attr("opacity", 0.6));

    const defs = svg.append("defs");
    data.forEach((d, i) => {
      const gradId = `bar-grad-${i}`;
      const grad = defs.append("linearGradient").attr("id", gradId).attr("x1", "0").attr("x2", "0").attr("y1", "0").attr("y2", "1");
      const c = d.color || "#6366f1";
      grad.append("stop").attr("offset", "0%").attr("stop-color", c).attr("stop-opacity", 1);
      grad.append("stop").attr("offset", "100%").attr("stop-color", c).attr("stop-opacity", 0.55);
    });

    g.selectAll("rect")
      .data(data)
      .enter()
      .append("rect")
      .attr("x", (d) => x(d.label)!)
      .attr("y", ih)
      .attr("width", x.bandwidth())
      .attr("height", 0)
      .attr("rx", 6)
      .attr("fill", (_, i) => `url(#bar-grad-${i})`)
      .transition()
      .duration(700)
      .attr("y", (d) => y(d.value))
      .attr("height", (d) => ih - y(d.value));
  }, [data, height, format]);

  return <svg ref={ref} className="w-full block text-foreground" />;
}

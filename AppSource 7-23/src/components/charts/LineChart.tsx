import { useEffect, useRef } from "react";
import * as d3 from "d3";

export interface Series {
  name: string;
  color: string;
  values: number[];
}

interface LineChartProps {
  labels: string[];
  series: Series[];
  height?: number;
}

export function LineChart({ labels, series, height = 260 }: LineChartProps) {
  const ref = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!ref.current) return;
    const svg = d3.select(ref.current);
    svg.selectAll("*").remove();

    const width = ref.current.clientWidth || 600;
    const margin = { top: 16, right: 12, bottom: 26, left: 36 };
    const iw = width - margin.left - margin.right;
    const ih = height - margin.top - margin.bottom;

    const x = d3.scalePoint<number>().domain(labels.map((_, i) => i)).range([0, iw]).padding(0.3);
    const allVals = series.flatMap((s) => s.values);
    const y = d3
      .scaleLinear()
      .domain([d3.min(allVals)! * 0.9, d3.max(allVals)! * 1.1])
      .range([ih, 0]);

    svg.attr("viewBox", `0 0 ${width} ${height}`).attr("width", "100%").attr("height", height);
    const g = svg.append("g").attr("transform", `translate(${margin.left},${margin.top})`);

    // grid
    g.append("g")
      .attr("class", "grid")
      .call(d3.axisLeft(y).ticks(4).tickSize(-iw).tickFormat(() => "") as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("line").attr("stroke", "currentColor").attr("opacity", 0.08));

    // axes
    g.append("g")
      .attr("transform", `translate(0,${ih})`)
      .call(d3.axisBottom(x).tickFormat((i) => labels[i as number]) as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("text").attr("fill", "currentColor").attr("font-size", 10).attr("opacity", 0.6));

    g.append("g")
      .call(d3.axisLeft(y).ticks(4).tickFormat((d) => d3.format(".2s")(d as number)) as any)
      .call((sel) => sel.select(".domain").remove())
      .call((sel) => sel.selectAll("line").remove())
      .call((sel) => sel.selectAll("text").attr("fill", "currentColor").attr("font-size", 10).attr("opacity", 0.6));

    const line = d3
      .line<number>()
      .x((_, i) => x(i)!)
      .y((d) => y(d))
      .curve(d3.curveMonotoneX);

    series.forEach((s, si) => {
      const gradId = `line-grad-${si}-${Math.random().toString(36).slice(2, 6)}`;
      const defs = svg.append("defs");
      const grad = defs.append("linearGradient").attr("id", gradId).attr("x1", "0").attr("x2", "0").attr("y1", "0").attr("y2", "1");
      grad.append("stop").attr("offset", "0%").attr("stop-color", s.color).attr("stop-opacity", 0.35);
      grad.append("stop").attr("offset", "100%").attr("stop-color", s.color).attr("stop-opacity", 0);

      const area = d3
        .area<number>()
        .x((_, i) => x(i)!)
        .y0(ih)
        .y1((d) => y(d))
        .curve(d3.curveMonotoneX);

      g.append("path").datum(s.values).attr("d", area as any).attr("fill", `url(#${gradId})`);
      g.append("path")
        .datum(s.values)
        .attr("d", line)
        .attr("fill", "none")
        .attr("stroke", s.color)
        .attr("stroke-width", 2.2)
        .attr("stroke-linecap", "round");

      g.selectAll(`.dot-${si}`)
        .data(s.values)
        .enter()
        .append("circle")
        .attr("cx", (_, i) => x(i)!)
        .attr("cy", (d) => y(d))
        .attr("r", 3)
        .attr("fill", "white")
        .attr("stroke", s.color)
        .attr("stroke-width", 2);
    });
  }, [labels, series, height]);

  return (
    <div>
      <svg ref={ref} className="w-full block text-foreground" />
      <div className="flex flex-wrap gap-3 mt-2">
        {series.map((s) => (
          <div key={s.name} className="flex items-center gap-1.5 text-[11px] text-muted-foreground">
            <span className="inline-block size-2 rounded-full" style={{ background: s.color }} />
            {s.name}
          </div>
        ))}
      </div>
    </div>
  );
}

import { useEffect, useRef } from "react";
import * as d3 from "d3";

interface DonutProps {
  data: { label: string; value: number; color: string }[];
  size?: number;
  centerLabel?: string;
  centerValue?: string;
}

export function DonutChart({ data, size = 220, centerLabel, centerValue }: DonutProps) {
  const ref = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!ref.current) return;
    const svg = d3.select(ref.current);
    svg.selectAll("*").remove();
    const radius = size / 2;
    const inner = radius * 0.62;

    svg.attr("viewBox", `0 0 ${size} ${size}`).attr("width", size).attr("height", size);
    const g = svg.append("g").attr("transform", `translate(${radius},${radius})`);

    const pie = d3.pie<{ label: string; value: number; color: string }>().value((d) => d.value).sort(null);
    const arc = d3.arc<any>().innerRadius(inner).outerRadius(radius - 4).cornerRadius(6).padAngle(0.02);

    g.selectAll("path")
      .data(pie(data))
      .enter()
      .append("path")
      .attr("d", arc as any)
      .attr("fill", (d) => d.data.color)
      .attr("opacity", 0.9);

    if (centerValue) {
      g.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", -2)
        .attr("font-size", 22)
        .attr("font-weight", 600)
        .attr("fill", "currentColor")
        .text(centerValue);
    }
    if (centerLabel) {
      g.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", 16)
        .attr("font-size", 11)
        .attr("opacity", 0.6)
        .attr("fill", "currentColor")
        .text(centerLabel);
    }
  }, [data, size, centerLabel, centerValue]);

  return <svg ref={ref} className="text-foreground" />;
}

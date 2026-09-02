import { useEffect, useState } from "react";
import { Download, FileText, Layers, Cloud, Database, Sparkles, Shield, GitBranch, Workflow, Zap } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";

export default function ArchitecturePage() {
  const [markdown, setMarkdown] = useState<string>("");

  useEffect(() => {
    fetch("/ARCHITECTURE.md")
      .then((r) => r.text())
      .then(setMarkdown)
      .catch(() => setMarkdown(""));
  }, []);

  const handleDownloadMarkdown = () => {
    const blob = new Blob([markdown], { type: "text/markdown;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "Team-Intelligence-Hub-Architecture.md";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleDownloadHtml = () => {
    const html = `<!doctype html><html><head><meta charset="utf-8"><title>Team Intelligence Hub — Architecture</title>
<style>
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 900px; margin: 40px auto; padding: 0 24px; color: #1f2937; line-height: 1.6; }
  h1, h2, h3 { color: #4f46e5; margin-top: 2em; }
  h1 { border-bottom: 3px solid #4f46e5; padding-bottom: 8px; }
  h2 { border-bottom: 1px solid #e5e7eb; padding-bottom: 6px; }
  pre { background: #f8fafc; padding: 16px; border-radius: 12px; overflow-x: auto; font-size: 13px; border: 1px solid #e2e8f0; }
  code { background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-size: 0.9em; }
  pre code { background: none; padding: 0; }
  table { border-collapse: collapse; width: 100%; margin: 16px 0; }
  th, td { border: 1px solid #e5e7eb; padding: 8px 12px; text-align: left; }
  th { background: #f8fafc; font-weight: 600; }
  blockquote { border-left: 4px solid #a78bfa; padding-left: 16px; color: #6b7280; }
</style></head><body><pre style="white-space: pre-wrap; font-family: inherit; background: none; border: none; padding: 0;">${markdown
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")}</pre></body></html>`;
    const blob = new Blob([html], { type: "text/html;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "Team-Intelligence-Hub-Architecture.html";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handlePrint = () => window.print();

  const sections = [
    { icon: Layers, title: "Architectural Layers", desc: "Five clean layers: Ingestion, Storage, Processing, Intelligence, API" },
    { icon: Database, title: "Data Model", desc: "Dataverse tables for Initiatives, People, Metrics, Evidence, Approvals" },
    { icon: Cloud, title: "File Storage", desc: "SharePoint for Office docs, Azure Blob for media, AI Search for vectors" },
    { icon: Workflow, title: "Processing Pipeline", desc: "Extract → Normalize → Chunk → Embed → Enrich → Index" },
    { icon: Sparkles, title: "AI Intelligence", desc: "Azure OpenAI + AI Search for RAG across all features" },
    { icon: GitBranch, title: "API Surface", desc: "Resource-oriented endpoints with streaming SSE for AI" },
    { icon: Shield, title: "Governance", desc: "Approval gates, Purview labels, external-safe content routing" },
    { icon: Zap, title: "Implementation Roadmap", desc: "Six phases across 13–16 weeks to production" },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-indigo-50/40">
      <div className="max-w-6xl mx-auto px-6 py-10">
        {/* Hero */}
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-indigo-600 via-purple-600 to-pink-500 p-10 text-white shadow-xl mb-10">
          <div className="absolute inset-0 opacity-20 bg-[radial-gradient(circle_at_20%_20%,white,transparent_40%),radial-gradient(circle_at_80%_60%,white,transparent_45%)]" />
          <div className="relative flex items-start justify-between gap-6 flex-wrap">
            <div className="max-w-2xl">
              <Badge className="bg-white/20 text-white border-white/30 backdrop-blur mb-4">Architecture Documentation</Badge>
              <h1 className="text-4xl font-bold tracking-tight mb-3">Team Intelligence Hub</h1>
              <p className="text-lg text-white/90 leading-relaxed">
                Complete backend architecture, AI integration design, data model, and
                implementation roadmap — ready to hand to your engineering team.
              </p>
            </div>
            <div className="flex flex-col gap-2 min-w-[220px]">
              <Button onClick={handleDownloadMarkdown} className="bg-white text-indigo-700 hover:bg-white/90" size="lg">
                <Download className="w-4 h-4 mr-2" /> Download Markdown
              </Button>
              <Button onClick={handleDownloadHtml} variant="outline" className="bg-white/10 border-white/30 text-white hover:bg-white/20" size="lg">
                <FileText className="w-4 h-4 mr-2" /> Download HTML
              </Button>
              <Button onClick={handlePrint} variant="ghost" className="text-white hover:bg-white/10" size="sm">
                Print / Save as PDF
              </Button>
            </div>
          </div>
        </div>

        {/* Section overview */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-10">
          {sections.map((s) => (
            <Card key={s.title} className="border-slate-200/60 shadow-sm hover:shadow-md transition rounded-2xl">
              <CardHeader className="pb-3">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-500 flex items-center justify-center mb-2">
                  <s.icon className="w-5 h-5 text-white" />
                </div>
                <CardTitle className="text-base">{s.title}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground leading-relaxed">{s.desc}</p>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Document preview */}
        <Card className="rounded-2xl border-slate-200/60 shadow-sm">
          <CardHeader>
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="w-5 h-5 text-indigo-600" />
                  Full Architecture Document
                </CardTitle>
                <CardDescription>
                  18 sections · covers backend, storage, AI, APIs, governance, roadmap
                </CardDescription>
              </div>
              <Badge variant="secondary" className="rounded-full">v1.0</Badge>
            </div>
          </CardHeader>
          <Separator />
          <CardContent className="pt-6">
            <div className="bg-slate-50 rounded-xl p-6 max-h-[600px] overflow-y-auto border border-slate-200">
              <pre className="whitespace-pre-wrap text-xs text-slate-700 font-mono leading-relaxed">
{markdown || "Loading architecture document…"}
              </pre>
            </div>
            <div className="mt-6 flex gap-3 flex-wrap">
              <Button onClick={handleDownloadMarkdown} className="bg-indigo-600 hover:bg-indigo-700">
                <Download className="w-4 h-4 mr-2" /> Download .md
              </Button>
              <Button onClick={handleDownloadHtml} variant="outline">
                <Download className="w-4 h-4 mr-2" /> Download .html
              </Button>
              <Button onClick={handlePrint} variant="outline">
                <FileText className="w-4 h-4 mr-2" /> Print / PDF
              </Button>
            </div>
            <p className="text-xs text-muted-foreground mt-4">
              Tip: use "Print / PDF" and choose <em>Save as PDF</em> in the print dialog for a
              polished, shareable version.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

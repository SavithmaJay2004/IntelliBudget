using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

namespace IntelliBudget {
public sealed partial class DashboardForm {
 sealed class Line {public string Category;public decimal Planned,Actual;}
 static decimal Total(DataTable rows,string kind)=>rows.Rows.Cast<DataRow>().Where(r=>(string)r["TransactionType"]==kind).Sum(r=>(decimal)r["Amount"]);
 static List<Line> Compare(Store db,DataRow budget,DataTable rows){
  var lines=new Dictionary<string,Line>(StringComparer.OrdinalIgnoreCase);
  Func<string,Line> get=name=>{Line line;if(!lines.TryGetValue(name,out line)){line=new Line{Category=name};lines.Add(name,line);}return line;};
  if(budget!=null){int id=(int)budget["BudgetID"];
   foreach(DataRow r in db.Entries("FixedExpenses",id).Rows)get((string)r["Category"]).Planned+=(decimal)r["Amount"];
   foreach(DataRow r in db.Entries("BudgetAllocations",id).Rows)get((string)r["Category"]).Planned+=(decimal)r["AllocatedAmount"];
  }
  foreach(DataRow r in rows.Rows)if((string)r["TransactionType"]=="Expense")get((string)r["Category"]).Actual+=(decimal)r["Amount"];
  return lines.Values.OrderBy(x=>x.Category).ToList();
 }
 static string Status(Line l)=>l.Actual==0?"":l.Planned==0?"Unplanned spending":l.Actual>l.Planned?"Over budget":l.Actual>=l.Planned*0.9m?"At least 90% used":"";
 static DataTable Comparison(IEnumerable<Line> lines){
  var t=new DataTable();t.Columns.Add("Category");t.Columns.Add("Planned LKR",typeof(decimal));t.Columns.Add("Actual LKR",typeof(decimal));t.Columns.Add("Balance LKR",typeof(decimal));t.Columns.Add("Alert");
  foreach(var l in lines)t.Rows.Add(l.Category,l.Planned,l.Actual,l.Planned-l.Actual,Status(l));return t;
 }
 void Overview(TableLayoutPanel page,Store db,DataRow budget,Plan plan){
  var rows=db.Transactions(user,Month,Month.AddMonths(1));var lines=Compare(db,budget,rows);
  if(budget==null)page.Controls.Add(UI.Caption("No plan for this month yet. Start with Planned income."));
  var cards=new TableLayoutPanel{Dock=DockStyle.Top,Height=112,ColumnCount=4,RowCount=1,Margin=new Padding(0,0,0,10)};
  cards.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  for(int i=0;i<4;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
  cards.Controls.Add(Metric("Planned income",plan.Income,UI.Blue),0,0);
  cards.Controls.Add(Metric("Unallocated",plan.Remaining,plan.Remaining<0?UI.Amber:UI.Accent),1,0);
  cards.Controls.Add(Metric("Actual income",Total(rows,"Income"),UI.Accent),2,0);
  cards.Controls.Add(Metric("Actual spending",Total(rows,"Expense"),UI.Amber),3,0);
  page.Controls.Add(cards);
  page.Controls.Add(UI.Caption("Fixed expenses: "+UI.Money(plan.Fixed)+"   •   Reserved savings: "+UI.Money(plan.Savings)));
  if(plan.Remaining<0)page.Controls.Add(UI.Label("The current plan exceeds planned income.",13));
  var alerts=lines.Where(l=>Status(l)!="").ToArray();page.Controls.Add(UI.Label("Spending alerts",17));
  if(alerts.Length==0)page.Controls.Add(UI.Label("No spending alerts for this month."));else page.Controls.Add(Grid(Comparison(alerts)));
 }
 static Control Metric(string title,decimal value,Color color){
  var panel=new TableLayoutPanel{
   Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=UI.Surface,Margin=new Padding(4,2,4,4)
  };
  panel.RowStyles.Add(new RowStyle(SizeType.Absolute,3));
  panel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
  panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  panel.Controls.Add(new Panel{Dock=DockStyle.Fill,BackColor=color,Margin=Padding.Empty},0,0);
  var label=UI.Caption(title);
  label.Margin=new Padding(12,12,0,0);
  panel.Controls.Add(label,0,1);
  var amount=new Label{
   Text=UI.Money(value),ForeColor=UI.Text,Font=new Font("Segoe UI Semibold",15),
   Dock=DockStyle.Fill,AutoEllipsis=true,Margin=new Padding(12,0,10,8)
  };
  panel.Controls.Add(amount,0,2);
  var details=new ToolTip();
  details.SetToolTip(amount,amount.Text);
  panel.Disposed+=(sender,args)=>details.Dispose();
  return panel;
 }
 void Reports(TableLayoutPanel page,Store db,DataRow budget,Plan plan){
  var rows=db.Transactions(user,Month,Month.AddMonths(1));var lines=Compare(db,budget,rows);
  var actions=UI.Flow();page.Controls.Add(actions);var periodName=Month.ToString("yyyy-MM");
  actions.Controls.Add(UI.Button("Export CSV",()=>Save("CSV files|*.csv","transactions-"+periodName,path=>Exports.Csv(path,rows)),true));
  actions.Controls.Add(UI.Button("Export PDF",()=>Save("PDF files|*.pdf","report-"+periodName,path=>{
   var output=new List<string>{"IntelliBudget | Monthly report | "+periodName,"All amounts in LKR","",
    "Planned income: "+UI.Money(plan.Income),"Fixed expenses: "+UI.Money(plan.Fixed),"Reserved savings: "+UI.Money(plan.Savings),"Unallocated: "+UI.Money(plan.Remaining),
    "Actual income: "+UI.Money(Total(rows,"Income")),"Actual spending: "+UI.Money(Total(rows,"Expense")),"","Category | Planned | Actual | Alert"};
   output.AddRange(lines.Select(l=>l.Category+" | "+l.Planned.ToString("N2")+" | "+l.Actual.ToString("N2")+" | "+Status(l)));
   Exports.Pdf(path,output);
  }),true));
  var chart=new Chart{Dock=DockStyle.Fill,Height=268,Margin=new Padding(4,8,4,10),BackColor=UI.Surface,Palette=ChartColorPalette.None,PaletteCustomColors=new[]{UI.Blue,UI.Accent}};
  var area=new ChartArea{BackColor=UI.Surface};chart.ChartAreas.Add(area);
  foreach(var axis in new[]{area.AxisX,area.AxisY}){axis.LabelStyle.ForeColor=UI.Text;axis.LineColor=UI.Muted;axis.MajorGrid.LineColor=UI.Border;}
  area.AxisX.Interval=1;area.AxisX.LabelStyle.Angle=-30;area.AxisY.LabelStyle.Format="N0";area.AxisY.Title="LKR";area.AxisY.TitleForeColor=UI.Text;
  chart.Legends.Add(new Legend{BackColor=UI.Surface,ForeColor=UI.Text,Docking=Docking.Top});
  foreach(var name in new[]{"Planned","Actual"})chart.Series.Add(new Series(name){ChartType=SeriesChartType.Column});
  foreach(var l in lines.OrderByDescending(l=>Math.Max(l.Planned,l.Actual)).Take(8)){chart.Series[0].Points.AddXY(l.Category,(double)l.Planned);chart.Series[1].Points.AddXY(l.Category,(double)l.Actual);}
  page.Controls.Add(UI.Label(lines.Count==0?"No spending categories in this month.":"Planned vs actual spending (up to 8 largest categories)"));
  if(lines.Count>0){page.Controls.Add(chart);var grid=Grid(Comparison(lines));grid.Height=170;page.Controls.Add(grid);}else chart.Dispose();
 }
 void Save(string filter,string filename,Action<string> write){
  using(var dialog=new SaveFileDialog{Filter=filter,FileName=filename,AddExtension=true})if(dialog.ShowDialog(this)==DialogResult.OK){write(dialog.FileName);MessageBox.Show(this,"Export saved.","IntelliBudget");}
 }
}
internal static class Exports{
 static string Cell(string value){
  string trimmed=value.TrimStart();if(trimmed.Length>0&&"=+-@".Contains(trimmed[0]))value="'"+value;
  return "\""+value.Replace("\"","\"\"")+"\"";
 }
 public static void Csv(string path,DataTable rows){
  using(var w=new StreamWriter(path,false,new UTF8Encoding(true))){
   w.WriteLine("Date,Type,Category,Amount (LKR),Notes");
   foreach(DataRow r in rows.Rows)w.WriteLine(string.Join(",",((DateTime)r["TransactionDate"]).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),Cell((string)r["TransactionType"]),Cell((string)r["Category"]),((decimal)r["Amount"]).ToString("0.00",CultureInfo.InvariantCulture),Cell(Convert.ToString(r["Notes"]))));
  }
 }
 // A dependency-free PDF writer for printable monthly summaries.
 public static void Pdf(string path,IEnumerable<string> lines){
  var wrapped=new List<string>();
  foreach(string raw in lines){
   string line=new string(raw.Select(c=>c>=32&&c<=126?c:'?').ToArray());
   while(line.Length>82){wrapped.Add(line.Substring(0,82));line=line.Substring(82);}wrapped.Add(line);
  }
  int count=(wrapped.Count+47)/48;
  using(var stream=File.Create(path)){
   var offsets=new List<long>{0};
   Action<string> write=s=>{var bytes=Encoding.ASCII.GetBytes(s);stream.Write(bytes,0,bytes.Length);};
   Action<string> obj=s=>{offsets.Add(stream.Position);write(offsets.Count-1+" 0 obj\n"+s+"\nendobj\n");};
   write("%PDF-1.4\n");
   obj("<< /Type /Catalog /Pages 2 0 R >>");
   obj("<< /Type /Pages /Count "+count+" /Kids ["+string.Join(" ",Enumerable.Range(0,count).Select(i=>(4+2*i)+" 0 R"))+"] >>");
   obj("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
   for(int i=0;i<count;i++){
    obj("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F 3 0 R >> >> /Contents "+(5+2*i)+" 0 R >>");
    var body=new StringBuilder("BT /F 10 Tf 14 TL 42 790 Td\n");
    foreach(string line in wrapped.Skip(i*48).Take(48))body.Append("(").Append(line.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)")).Append(") Tj T*\n");
    body.Append("T* (Page ").Append(i+1).Append(" of ").Append(count).Append(") Tj ET\n");
    obj("<< /Length "+body.Length+" >>\nstream\n"+body+"endstream");
   }
   long xref=stream.Position;write("xref\n0 "+offsets.Count+"\n0000000000 65535 f \n");
   foreach(long offset in offsets.Skip(1))write(offset.ToString("D10")+" 00000 n \n");
   write("trailer\n<< /Size "+offsets.Count+" /Root 1 0 R >>\nstartxref\n"+xref+"\n%%EOF\n");
  }
 }
}
}

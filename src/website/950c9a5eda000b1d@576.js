function _1(md, colorout, colorin) {
  return (
    md`

# Heroes of the Storm - Hero Relationships

Displays the relationships between <b style="color: ${colorout};">Heroes</b> and various categories such as <b style="color: ${colorin};">cc, difficulty, energy, mechanics, play style, roles, complexity, radius sizes, etc</b>. All data is extracted from game data, nothing is custom or assigned a relationship by personal opinion.

Labels with an Asterix (*) may have various meanings, as the data only confirms if keywords exist on the hero metadata. So take that into consideration.

`
  )
}

function _chart(d3, bilink, data, id, colornone, colorin, colorout) {
  const width = 954;
  const radius = width / 2;

  const tree = d3.cluster()
    .size([2 * Math.PI, radius - 100]);
  const root = tree(bilink(d3.hierarchy(data)
    .sort((a, b) => d3.ascending(a.height, b.height) || d3.ascending(a.data.name, b.data.name))));

  const svg = d3.create("svg")
    .attr("width", width)
    .attr("height", width)
    .attr("viewBox", [-width / 2, -width / 2, width, width])
    .attr("style", "max-width: 100%; height: auto; font: 10px sans-serif;");

  const node = svg.append("g")
    .selectAll("g")
    .data(root.leaves())
    .join("g")
    .attr("transform", d => `rotate(${d.x * 180 / Math.PI - 90}) translate(${d.y},0)`)
    .append("text")
    .attr("dy", "0.31em")
    .attr("x", d => d.x < Math.PI ? 6 : -6)
    .attr("text-anchor", d => d.x < Math.PI ? "start" : "end")
    .attr("transform", d => d.x >= Math.PI ? "rotate(180)" : null)
    .text(d => d.data.name)
    .each(function (d) { d.text = this; })
    .on("mouseover", overed)
    .on("mouseout", outed)
    .call(text => text.append("title").text(d => `${id(d)}
${d.outgoing.length} outgoing
${d.incoming.length} incoming`));

  const line = d3.lineRadial()
    .curve(d3.curveBundle.beta(0.85))
    .radius(d => d.y)
    .angle(d => d.x);

  const link = svg.append("g")
    .attr("stroke", colornone)
    .attr("fill", "none")
    .selectAll("path")
    .data(root.leaves().flatMap(leaf => leaf.outgoing))
    .join("path")
    .style("mix-blend-mode", "multiply")
    .attr("d", ([i, o]) => line(i.path(o)))
    .each(function (d) { d.path = this; });

  function overed(event, d) {
    link.style("mix-blend-mode", null);
    d3.select(this).attr("font-weight", "bold");
    d3.selectAll(d.incoming.map(d => d.path)).attr("stroke", colorin).raise();
    d3.selectAll(d.incoming.map(([d]) => d.text)).attr("fill", colorin).attr("font-weight", "bold");
    d3.selectAll(d.outgoing.map(d => d.path)).attr("stroke", colorout).raise();
    d3.selectAll(d.outgoing.map(([, d]) => d.text)).attr("fill", colorout).attr("font-weight", "bold");
  }

  function outed(event, d) {
    link.style("mix-blend-mode", "multiply");
    d3.select(this).attr("font-weight", null);
    d3.selectAll(d.incoming.map(d => d.path)).attr("stroke", null);
    d3.selectAll(d.incoming.map(([d]) => d.text)).attr("fill", null).attr("font-weight", null);
    d3.selectAll(d.outgoing.map(d => d.path)).attr("stroke", null);
    d3.selectAll(d.outgoing.map(([, d]) => d.text)).attr("fill", null).attr("font-weight", null);
  }

  return svg.node();
}


async function _data(hierarchy, FileAttachment) {
  return (
    hierarchy(await FileAttachment("data.json").json())
  )
}

function _hierarchy() {
  return (
    function hierarchy(data, delimiter = ".") {
      let root;
      const map = new Map;
      data.forEach(function find(data) {
        const { name } = data;
        if (map.has(name)) return map.get(name);
        const i = name.lastIndexOf(delimiter);
        map.set(name, data);
        if (i >= 0) {
          find({ name: name.substring(0, i), children: [] }).children.push(data);
          data.name = name.substring(i + 1);
        } else {
          root = data;
        }
        return data;
      });
      return root;
    }
  )
}

function _bilink(id) {
  return (
    function bilink(root) {
      const map = new Map(root.leaves().map(d => [id(d), d]));
      for (const d of root.leaves()) d.incoming = [], d.outgoing = d.data.imports.map(i => [d, map.get(i)]);
      for (const d of root.leaves()) for (const o of d.outgoing) o[1].incoming.push(o);
      return root;
    }
  )
}

function _id() {
  return (
    function id(node) {
      return `${node.parent ? id(node.parent) + "." : ""}${node.data.name}`;
    }
  )
}

function _colorin() {
  return (
    "#00f"
  )
}

function _colorout() {
  return (
    "#f00"
  )
}

function _colornone() {
  return (
    "#ccc"
  )
}

export default function define(runtime, observer) {
  const main = runtime.module();
  function toString() { return this.url; }
  const fileAttachments = new Map([
    ["data.json", { url: new URL("./files/data.json", import.meta.url), mimeType: "application/json", toString }]
  ]);
  main.builtin("FileAttachment", runtime.fileAttachments(name => fileAttachments.get(name)));
  main.variable(observer()).define(["md", "colorout", "colorin"], _1);
  main.variable(observer("chart")).define("chart", ["d3", "bilink", "data", "id", "colornone", "colorin", "colorout"], _chart);
  main.variable(observer("data")).define("data", ["hierarchy", "FileAttachment"], _data);
  main.variable(observer("hierarchy")).define("hierarchy", _hierarchy);
  main.variable(observer("bilink")).define("bilink", ["id"], _bilink);
  main.variable(observer("id")).define("id", _id);
  main.variable(observer("colorin")).define("colorin", _colorin);
  main.variable(observer("colorout")).define("colorout", _colorout);
  main.variable(observer("colornone")).define("colornone", _colornone);
  return main;
}

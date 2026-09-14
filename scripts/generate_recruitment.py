"""Validate and serialize the editable historical recruitment rules."""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def build(payload):
    records=payload['generals']
    if sorted(r['id'] for r in records)!=list(range(255)):
        raise ValueError('Rules must cover each original general ID exactly once')
    root=ET.Element('Recruitment',schemaVersion='1')
    for r in sorted(records,key=lambda r:r['id']):
        general=ET.SubElement(root,'General',id=str(r['id']),Name=r['name'],evidence=r['evidence'],note=r['note'])
        previous_end=-1
        for s in r['stages']:
            start,end=s['fromYear'],s['toYear']
            if not (0<=start<=end<=9999 and start>previous_end):
                raise ValueError(f'{r["name"]}: overlapping or invalid year ranges')
            if not s['cities'] or len(s['cities'])!=len(set(s['cities'])) or any(not 0<=i<48 for i in s['cities']):
                raise ValueError(f'{r["name"]}: invalid city IDs')
            if len(s['rulers'])!=len(set(s['rulers'])) or any(not 0<=i<255 for i in s['rulers']):
                raise ValueError(f'{r["name"]}: invalid ruler IDs')
            attrs=dict(fromYear=str(start),toYear=str(end),rulers=','.join(map(str,s['rulers'])),cities=','.join(map(str,s['cities'])))
            ET.SubElement(general,'Stage',**attrs)
            previous_end=end
        for source in r.get('sources',[]):
            ET.SubElement(general,'Source',url=source['url'],title=source['title'])
    ET.indent(root,space='  ')
    return ET.tostring(root,encoding='utf-8',xml_declaration=True)+b'\n'


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config',type=Path,default=Path(__file__).resolve().parents[1]/'scenarios/historical_recruitment.json')
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    payload=json.loads(args.config.read_text(encoding='utf-8-sig'))
    xml=build(payload)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_bytes(xml)
    print(f'{len(payload["generals"])} general rules; {sum(len(r["stages"]) for r in payload["generals"])} historical stages')


if __name__=='__main__':main()

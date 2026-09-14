"""Build original scenario layouts over a user's locally extracted game templates.

No game binary, art, signing key, or extracted base XML is distributed here.
python -X utf8 scripts/generate_scenarios.py --base-dir local/base --output-dir build/scenarios
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def require(condition, message):
    if not condition:
        raise ValueError(message)


def load_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def build(config, base_dir, names, bounds):
    city_names = names['cityName']
    general_names = names['generalName']
    city_id = {n: i for i, n in enumerate(city_names)}
    general_id = {n: i for i, n in enumerate(general_names)}
    require(len(city_names) == 48 and len(general_names) == 255, 'Expected original 48 cities / 255 generals')
    factions = config['factions']
    require(2 <= len(factions) <= 9, 'Each new scenario supports 2..9 selectable factions')
    require(config['selectableCount'] == len(factions), 'All scenario factions must be selectable')
    root = ET.parse(base_dir / config['template']).getroot()
    root.attrib.update(Name=config['name'], Year=str(config['year']), SelectableCount=str(len(factions)), Kind=config['kind'])
    kings = root.find('King')
    kings.clear()
    cities = list(root.find('City'))
    generals = list(root.find('General'))
    require(len(cities) == 48 and len(generals) == 255, 'Wrong template size')
    used_cities = set()
    used_generals = set()
    economy = config['economy']
    for i, city in enumerate(cities):
        city.set('Index', str(i))
        city.set('King', '-1')
        city.set('Name', city_names[i])
        city.set('Money', str(economy['money'] // 2))
        city.set('Reservist', '0')
        city.set('Defense', str(economy['defense'] // 2))
    for i, general in enumerate(generals):
        general.set('Index', str(i))
        general.set('King', '-1')
        general.set('City', '-1')
        general.set('Active', '0')
        general.set('Available', '1' if config['kind'] == '架空' else '0')
        # Preserve the template's equipment-adjusted stats and valid skill masks.
        # All start fresh, including generals that were off-map in that template.
        for current, maximum in [('HealthCur','HealthMax'),('ManaCur','ManaMax'),('SoldierCur','SoldierMax'),('KnightCur','KnightMax')]:
            general.set(current, general.get(maximum))

    faction_report = []
    for king_index, faction in enumerate(factions):
        leader = faction['leader']
        roster = faction['roster']
        require(leader in roster, f'{leader}: ruler missing from roster')
        require(faction['capital'] in faction['cities'], f'{leader}: capital not owned')
        require(len(roster) == len(set(roster)), f'{leader}: duplicate roster names')
        require(len(faction['cities']) <= len(roster) <= 10 * len(faction['cities']), f'{leader}: not enough garrisons / too many generals')
        require(not used_cities.intersection(faction['cities']), f'{leader}: duplicated city')
        require(not used_generals.intersection(roster), f'{leader}: duplicated general: {used_generals.intersection(roster)}')
        require(all(n in general_id for n in roster), f'{leader}: unknown general')
        require(all(c in city_id for c in faction['cities']), f'{leader}: unknown city')
        require(all(c in faction['cities'] for c in faction['frontier']), f'{leader}: frontier must be owned')
        used_cities.update(faction['cities'])
        used_generals.update(roster)
        ET.SubElement(kings, 'Item', Index=str(king_index), Name=leader, Active='1', GeneralIdx=str(general_id[leader]))
        slots = {c: [] for c in faction['cities']}
        slots[faction['capital']].append(leader)
        anchored = {leader}
        for city, group in faction['anchors'].items():
            require(city in slots, f'{leader}: anchor city not owned: {city}')
            for name in group:
                require(name in roster, f'{leader}: unknown anchor {name}')
                if name == leader and city == faction['capital']:
                    continue
                require(name not in anchored, f'{leader}: duplicate anchor {name}')
                require(len(slots[city]) < 10, f'{leader}: full anchor city {city}')
                slots[city].append(name)
                anchored.add(name)
        remaining = [n for n in roster if n not in anchored]
        # First fill every owned city, then assemble front-line groups. Deep
        # rear cities retain at least one general for the existing game's rules.
        for city in slots:
            if not slots[city]:
                slots[city].append(remaining.pop(0))
        for city in faction['frontier']:
            while len(slots[city]) < 4 and remaining:
                slots[city].append(remaining.pop(0))
        while remaining:
            options = [c for c in slots if len(slots[c]) < 10]
            require(options, f'{leader}: city capacity exceeded')
            target = min(options, key=lambda c: (len(slots[c]), c != faction['capital'], city_id[c]))
            slots[target].append(remaining.pop(0))
        for city_name, garrison in slots.items():
            city = cities[city_id[city_name]]
            capital = city_name == faction['capital']
            city.set('King', str(king_index))
            city.set('Money', str(faction.get('capitalMoney' if capital else 'money', faction.get('money', economy['capitalMoney' if capital else 'money']))))
            city.set('Defense', str(faction.get('capitalDefense' if capital else 'defense', faction.get('defense', economy['capitalDefense' if capital else 'defense']))))
            reserve = faction.get('reservist', economy['reservist'])
            city.set('ReservistMax', str(max(reserve, int(city.get('ReservistMax')))))
            city.set('Reservist', str(reserve))
            for name in garrison:
                general = generals[general_id[name]]
                general.set('King', str(king_index))
                general.set('City', str(city_id[city_name]))
                general.set('Active', '1')
                general.set('Available', '1')
        faction_report.append(dict(name=leader, cities=len(slots), generals=len(roster), garrisons=slots))
    for name in config['freeGenerals']:
        require(name in general_id, f'Unknown free general {name}')
        require(name not in used_generals, f'Duplicate free general {name}')
        generals[general_id[name]].set('Available', '1')
        generals[general_id[name]].set('Active', '1')
        used_generals.add(name)
    if config['kind'] == '历史':
        for name in used_generals:
            require(config['year'] <= bounds['lastAvailableYear'].get(name,999), f'{config["name"]}: deceased {name}')
            require(config['year'] >= bounds['firstAvailableYear'].get(name,180), f'{config["name"]}: too early for {name}')
    validate_xml(root)
    ET.indent(root, space='  ')
    xml = ET.tostring(root, encoding='utf-8', xml_declaration=True) + b'\n'
    report = dict(index=config['index'], name=config['name'], year=config['year'], kind=config['kind'], factionCount=len(factions), availableGenerals=sum(g.get('Available') == '1' for g in generals), ownedCities=len(used_cities), neutralCities=[n for n in city_names if n not in used_cities], factions=faction_report, sha256=hashlib.sha256(xml).hexdigest())
    return xml, report


def validate_xml(root):
    kings, cities, generals = [list(root.find(n)) for n in ('King', 'City', 'General')]
    require(len(cities)==48 and len(generals)==255, 'XML entity counts invalid')
    require(len(kings)==int(root.get('SelectableCount')), 'XML selectable count mismatch')
    for i, general in enumerate(generals):
        require(int(general.get('Index')) == i, f'General order {i}')
        k,c = int(general.get('King')),int(general.get('City'))
        require((-1 <= k < len(kings)) and (-1 <= c < 48), f'General {i}: illegal affiliation')
        require((k == -1)==(c == -1), f'General {i}: partial affiliation')
        if k>=0:
            require(int(cities[c].get('King'))==k and general.get('Available')=='1', f'General {i}: wrong faction/city')
        for cur,mx in [('HealthCur','HealthMax'),('ManaCur','ManaMax'),('SoldierCur','SoldierMax'),('KnightCur','KnightMax')]:
            require(0<=int(general.get(cur))<=int(general.get(mx)), f'General {i}: invalid {cur}')
        require(0<int(general.get('Arms'))<2048 and 0<int(general.get('Formation'))<256, f'General {i}: invalid mask')
        require(-1<=int(general.get('Equipment'))<30, f'General {i}: invalid equipment')
    for i,city in enumerate(cities):
        k=int(city.get('King'))
        occupants=[g for g in generals if int(g.get('City'))==i]
        require((-1<=k<len(kings)) and len(occupants)<=10, f'City {i}: invalid ownership/capacity')
        require((k==-1)==(not occupants), f'City {i}: owned but empty / neutral but occupied')
        require(0<=int(city.get('Reservist'))<=int(city.get('ReservistMax')), f'City {i}: invalid reserves')
        require(0<=int(city.get('Money'))<=999999, f'City {i}: invalid money')
        require(0<=int(city.get('Defense'))<=9999, f'City {i}: invalid defense')
    for k,king in enumerate(kings):
        ruler=generals[int(king.get('GeneralIdx'))]
        require(int(ruler.get('King'))==k and int(ruler.get('City'))>=0, f'King {k}: ruler not in own city')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-dir',type=Path,required=True)
    parser.add_argument('--output-dir',type=Path,required=True)
    parser.add_argument('--config-dir',type=Path,default=Path(__file__).resolve().parents[1]/'scenarios')
    args=parser.parse_args()
    names=load_json(args.base_dir/'names.json')
    bounds=load_json(args.config_dir/'roster_bounds.json')
    reports=[]
    args.output_dir.mkdir(parents=True,exist_ok=True)
    for path in sorted(args.config_dir.glob('[0-9][0-9]-*.json')):
        config=load_json(path)
        xml,report=build(config,args.base_dir,names,bounds)
        (args.output_dir/f'MOD{config["index"]+1:02d}.xml').write_bytes(xml)
        reports.append(report)
        print(f'{path.name}: {report["factionCount"]} factions, {report["ownedCities"]} owned cities, {report["availableGenerals"]} available generals')
    (args.output_dir/'scenario-report.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2)+'\n',encoding='utf8')


if __name__=='__main__':
    main()

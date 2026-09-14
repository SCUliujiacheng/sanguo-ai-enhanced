"""Data regression checks, runnable with only Python's standard library.

python -m unittest discover -s scripts -p test_data.py -v
Scenario build checks require locally extracted templates in local/base.
"""
import os
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET
import generate_scenarios as generate
import generate_recruitment

ROOT=Path(__file__).resolve().parents[1]
CONFIG=ROOT/'scenarios'
BASE=Path(os.environ.get('SANGUO_BASE_DIR', str(ROOT/'local/base')))


class RecruitmentDataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.payload=json.loads((CONFIG/'historical_recruitment.json').read_text(encoding='utf8'))
        cls.records={r['name']:r for r in cls.payload['generals']}

    def stage_at(self,name,year):
        return next((s for s in self.records[name]['stages'] if s['fromYear']<=year<=s['toYear']),None)

    def test_all_original_ids_have_explicit_rules(self):
        self.assertEqual(sorted(r['id'] for r in self.records.values()),list(range(255)))
        self.assertEqual(len(ET.fromstring(generate_recruitment.build(self.payload)).findall('General')),255)

    def test_zhuge_liang_has_207_start_and_234_stop(self):
        self.assertIsNone(self.stage_at('诸葛亮',206))
        self.assertEqual(self.stage_at('诸葛亮',207)['rulers'],[134,136])
        self.assertIn(22,self.stage_at('诸葛亮',207)['cities'])
        self.assertNotIn(18,self.stage_at('诸葛亮',207)['cities'])
        self.assertIsNone(self.stage_at('诸葛亮',235))

    def test_xu_shu_changes_search_allegiance_in_208(self):
        self.assertEqual(self.stage_at('徐庶',207)['rulers'],[134,136])
        self.assertIn(98,self.stage_at('徐庶',208)['rulers'])
        self.assertNotIn(134,self.stage_at('徐庶',208)['rulers'])

    def test_huang_zhong_is_not_a_219_newcomer(self):
        self.assertEqual(self.stage_at('黄忠',200)['rulers'],[132])
        self.assertEqual(self.stage_at('黄忠',200)['cities'],[28])
        self.assertEqual(self.stage_at('黄忠',209)['rulers'],[134,136])
        self.assertIsNone(self.stage_at('黄忠',221))

    def test_wei_yan_uses_attested_shu_entry_not_romance_service(self):
        self.assertIsNone(self.stage_at('魏延',210))
        self.assertEqual(self.stage_at('魏延',211)['rulers'],[134,136])
        self.assertIn(37,self.stage_at('魏延',219)['cities'])

    def test_jiang_wei_keeps_battle_eve_228_wei_then_229_shu(self):
        self.assertIsNone(self.stage_at('姜维',219))
        self.assertIn(99,self.stage_at('姜维',228)['rulers'])
        self.assertIn(14,self.stage_at('姜维',228)['cities'])
        self.assertEqual(self.stage_at('姜维',229)['rulers'],[134,136])
        self.assertIsNone(self.stage_at('姜维',265))

    def test_late_generation_cannot_be_found_as_children(self):
        for name in ['司马炎','钟会','陆抗','文鸯','司马师','司马昭']:
            with self.subTest(name=name):
                self.assertIsNone(self.stage_at(name,208))
                self.assertIsNone(self.stage_at(name,219))
                self.assertTrue(self.records[name]['stages'])
        self.assertIsNone(self.stage_at('司马炎',250))
        self.assertIsNotNone(self.stage_at('司马炎',260))

    def test_early_dead_generals_do_not_reenter_late_search(self):
        for name in ['典韦','郭嘉','荀彧','曹操','关羽','张飞','刘备','曹丕','吕布','董卓']:
            with self.subTest(name=name):
                self.assertIsNone(self.stage_at(name,228))

    def test_fictional_names_are_explicitly_labeled_and_not_in_historical_pool(self):
        for name in ['貂蝉','祝融夫人','兀突骨','周仓','关索']:
            with self.subTest(name=name):
                self.assertEqual(self.records[name]['stages'],[])
                self.assertIn('演义',self.records[name]['evidence'])

    def test_absent_original_ruler_keeps_local_region(self):
        s=self.stage_at('黄忠',208)
        self.assertEqual(s['rulers'],[])
        self.assertEqual(s['cities'],[28])
        s=self.stage_at('贾诩',198)
        self.assertEqual(s['rulers'],[])
        self.assertEqual(s['cities'],[21,22])

    def test_overlapping_windows_are_rejected(self):
        invalid=copy.deepcopy(self.payload)
        invalid['generals'][0]['stages'].append(copy.deepcopy(invalid['generals'][0]['stages'][0]))
        with self.assertRaisesRegex(ValueError,'overlapping'):
            generate_recruitment.build(invalid)


@unittest.skipUnless((BASE/'names.json').exists(),'Requires locally extracted game templates')
class ScenarioBuildTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.names=generate.load_json(BASE/'names.json')
        cls.bounds=generate.load_json(CONFIG/'roster_bounds.json')
        cls.configs=[generate.load_json(p) for p in sorted(CONFIG.glob('[0-9][0-9]-*.json'))]

    def test_exact_nine_new_scenarios_and_resource_ids(self):
        self.assertEqual([c['index'] for c in self.configs],list(range(5,14)))
        for c in self.configs:
            self.assertEqual(c['resource'],f'Scenarios.MOD{c["index"]+1:02d}.xml')

    def test_all_scenarios_roundtrip_and_validate_full_rosters(self):
        for c in self.configs:
            with self.subTest(scenario=c['name']):
                xml,_=generate.build(c,BASE,self.names,self.bounds)
                root=ET.fromstring(xml)
                generate.validate_xml(root)
                self.assertEqual(root.get('Name'),c['name'])
                self.assertEqual(int(root.get('Year')),c['year'])

    def test_duplicate_general_between_factions_fails_before_packaging(self):
        c=copy.deepcopy(self.configs[0])
        c['factions'][1]['roster'].append(c['factions'][0]['roster'][1])
        with self.assertRaisesRegex(ValueError,'duplicated general'):
            generate.build(c,BASE,self.names,self.bounds)

    def test_222_and_228_do_not_start_with_child_sima_brothers(self):
        for c in self.configs:
            if c['kind']=='历史' and c['year']>=222:
                roster={n for f in c['factions'] for n in f['roster']}
                self.assertNotIn('司马师',roster)
                self.assertNotIn('司马昭',roster)

    def test_228_succession_and_battle_eve_rosters(self):
        c=next(c for c in self.configs if c['name']=='北伐中原')
        self.assertEqual([f['leader'] for f in c['factions']],['曹睿','刘禅','孙权'])
        self.assertIn('姜维',c['factions'][0]['roster'])
        self.assertNotIn('姜维',c['factions'][1]['roster'])
        roster={n for f in c['factions'] for n in f['roster']}
        for name in ['曹操','刘备','曹丕','关羽','张飞','周瑜','郭嘉']:
            self.assertNotIn(name,roster)

    def test_hero_assembly_has_equal_city_counts(self):
        c=next(c for c in self.configs if c['name']=='英雄集结')
        self.assertEqual([len(f['cities']) for f in c['factions']],[5]*9)

    def test_lone_city_has_escape_space_and_ten_generals(self):
        c=next(c for c in self.configs if c['name']=='孤城逆袭')
        self.assertEqual(c['factions'][0]['cities'],['新野'])
        self.assertEqual(len(c['factions'][0]['roster']),10)
        self.assertEqual(c['factions'][0]['capitalMoney'],10000)
        occupied={n for f in c['factions'] for n in f['cities']}
        self.assertNotIn('上庸',occupied)
        self.assertNotIn('武陵',occupied)


if __name__=='__main__':unittest.main()

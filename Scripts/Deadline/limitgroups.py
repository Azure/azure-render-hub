import sys
import argparse
import System
from Deadline.Scripting import RepositoryUtils

def add_slave_to_limit_group(slave, group_name, exclude):
    lg = RepositoryUtils.GetLimitGroup(group_name, True)
    if lg is None:
        print('The limit group {} was not found'.format(group_name))
        return
    if exclude:
        if group_name not in lg.LimitGroupExcludedSlaves:
            newlist = System.Collections.Generic.List[System.String](lg.LimitGroupExcludedSlaves)
            newlist.Add(slave)
            lg.SetLimitGroupExcludedSlaves(newlist.ToArray())
    else:
        if group_name not in lg.LimitGroupListedSlaves:
            newlist = System.Collections.Generic.List[System.String](lg.LimitGroupListedSlaves)
            newlist.Add(slave)
            lg.SetLimitGroupListedSlaves(slave)
    RepositoryUtils.SaveLimitGroup(lg)

def __main__(*args):
    parser = argparse.ArgumentParser()
    parser.add_argument('--limitgroups', nargs='+', help='One or more limit groups separated by a space')
    parser.add_argument('--slave', help='The slave name')
    parser.add_argument('--exclude', action='store_true', help='Add the ')
    parsed_args = parser.parse_args(args)
    slave = parsed_args.slave
    exclude = parsed_args.exclude
    for group_name in parsed_args.limitgroups:
        add_slave_to_limit_group(slave, group_name, exclude)

"""NEAT-Python topology evolution, species protection and homologous crossover."""
import dataclasses,json,random,contextlib,io
from pathlib import Path
from curves import value
from neat_policy import NeatPolicy,export
import tasks


def make_config(output,config):
    import neat
    p=config.schedules or {};n=max(2,int(value(p.get("population"),1,20)))
    settings={"NEAT":dict(fitness_criterion="max",fitness_threshold=1000000,pop_size=n,reset_on_extinction=True,no_fitness_termination=True),
      "DefaultGenome":dict(num_inputs=69,num_outputs=133,num_hidden=0,feed_forward=True,compatibility_disjoint_coefficient=1.,compatibility_weight_coefficient=.5,
        conn_add_prob=value(p.get("connection_add"),1,.3),conn_delete_prob=.05,node_add_prob=value(p.get("node_add"),1,.1),node_delete_prob=.02,
        single_structural_mutation=False,structural_mutation_surer="default",initial_connection="partial_direct 0.03",activation_default="tanh",activation_mutate_rate=0.,activation_options="tanh",aggregation_default="sum",aggregation_mutate_rate=0.,aggregation_options="sum",enabled_default=True,enabled_mutate_rate=.01),
      "DefaultSpeciesSet":dict(compatibility_threshold=value(p.get("compatibility"),1,3.)),
      "DefaultStagnation":dict(species_fitness_func="max",max_stagnation=15,species_elitism=1),
      "DefaultReproduction":dict(elitism=1,survival_threshold=.2,min_species_size=1)}
    for prefix in ("bias","response","weight"):
        settings["DefaultGenome"].update({prefix+"_"+k:v for k,v in dict(init_mean=1. if prefix=="response" else 0.,init_stdev=0. if prefix=="response" else 1.,replace_rate=.1 if prefix!="response" else 0.,mutate_rate=.8 if prefix!="response" else 0.,mutate_power=.5,max_value=30.,min_value=-30.).items()})
    # Pin every library default explicitly so upgrades cannot silently alter the experiment.
    for gene in (neat.genes.DefaultNodeGene,neat.genes.DefaultConnectionGene):
        for parameter in gene.get_config_params():
            if parameter.name not in settings["DefaultGenome"] and parameter.default is not None:settings["DefaultGenome"][parameter.name]=parameter.default
    file=output/"neat-config.ini";file.write_text("\n".join("["+section+"]\n"+"\n".join(f"{key} = {v}" for key,v in fields.items()) for section,fields in settings.items()),encoding="utf-8")
    return neat.Config(neat.DefaultGenome,neat.DefaultReproduction,neat.DefaultSpeciesSet,neat.DefaultStagnation,str(file))


def run(host,config,output,resume,emit,check_stop):
    import neat
    random.seed(config.seed);rng=random.Random(config.seed);configuration=make_config(output,config);marker=output/"neat-resume.json";history=[]
    if resume and marker.exists():
        saved=json.loads(marker.read_text(encoding="utf-8"));file=Path(saved["checkpoint"])
        if file.name!=str(file):raise ValueError("Invalid local checkpoint name.")
        population=neat.Checkpointer.restore_checkpoint(str(output/file),new_config=configuration);history=saved["history"]
        # Library restore resets reproduction IDs; preserve stable identity across resumed runs.
        import itertools
        population.reproduction.genome_indexer=itertools.count(saved["next_id"])
        rng.setstate(_tuple(saved["rng"]))
    else:population=neat.Population(configuration)
    policy=NeatPolicy(configuration);first=population.generation+1;enabled=[t for t in (config.tasks if config.tasks is not None else [dict(kind="tournament",parameters=dict(count=2,max_moves=20))]) if t.get("enabled",True)]
    if not enabled:raise ValueError("Enable at least one NEAT fitness task.")
    def evaluate(genomes,c):
        check_stop();round_number=population.generation+1;identities=[i for i,g in genomes];members=[g for i,g in genomes];fitness=[0.]*len(members)
        settings=dataclasses.replace(config,engine_ids=identities,round_fraction=(round_number-first)/max(1,config.rounds-1))
        emit(event="round_start",round=round_number,total=first+config.rounds-1)
        for index,task in enumerate(enabled):
            emit(event="task_start",task=task.get("name",task["kind"]),task_index=index+1,tasks=len(enabled))
            scores,_=tasks.execute(host,policy,members,task,settings,round_number,rng,emit,check_stop);fitness=[a+b for a,b in zip(fitness,scores)]
        for genome,score in zip(members,fitness):genome.fitness=score
        best=max(members,key=lambda g:g.fitness);export(neat.nn.FeedForwardNetwork.create(best,c),output/"best-neat.json")
        row=dict(round=round_number,best_engine=best.key,best_score=best.fitness,mean_score=sum(fitness)/len(fitness),species=len(population.species.species),nodes=len(best.nodes),connections=len(best.connections));history.append(row);emit(event="round_complete",**row)
    for _ in range(config.rounds):
        check_stop();population.run(evaluate,1)
        prefix=str(output/"neat-checkpoint-");checkpointer=neat.Checkpointer(1,filename_prefix=prefix)
        with contextlib.redirect_stdout(io.StringIO()):checkpointer.save_checkpoint(population.config,population.population,population.species,population.generation)
        # next(reproduction counter) consumes one unused ID; gaps are intentional and avoid ID reuse.
        pending=marker.with_suffix(".tmp");pending.write_text(json.dumps(dict(checkpoint="neat-checkpoint-"+str(population.generation),history=history,next_id=next(population.reproduction.genome_indexer),rng=rng.getstate())),encoding="utf-8");pending.replace(marker)
        (output/"history.json").write_text(json.dumps(history,indent=2),encoding="utf-8")
    from result_tasks import run as results
    report=dict(history=history,results=results(host,policy,config,output,rng,emit,check_stop,history[-1]["best_engine"]),model="neat",score_definition="Weighted chess task fitness; topology evolution and speciation run on CPU.")
    (output/"report.json").write_text(json.dumps(report,indent=2),encoding="utf-8");return report


def _tuple(x):return tuple(_tuple(v) for v in x) if isinstance(x,list) else x

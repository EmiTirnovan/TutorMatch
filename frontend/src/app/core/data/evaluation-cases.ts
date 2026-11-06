import { EvaluationCase } from '../models/semantic-search.models';

/**
 * Ground-truth set mirroring TutorMatch.Tests/Evaluation/SemanticSearchEvaluationTests.cs.
 * It assumes the demo tutors with these ids have been seeded.
 */
const TUTORS = {
  math: { id: '11111111-1111-1111-1111-111111111111', name: 'Ion Popescu', category: 'Matematica' },
  algorithms: { id: '22222222-2222-2222-2222-222222222222', name: 'Alexandru Radu', category: 'Informatica' },
  physics: { id: '33333333-3333-3333-3333-333333333333', name: 'Elena Dumitrescu', category: 'Fizica' },
  english: { id: '44444444-4444-4444-4444-444444444444', name: 'Maria Ionescu', category: 'Limba Engleza' },
  chemistry: { id: '55555555-5555-5555-5555-555555555555', name: 'Andrei Vasilescu', category: 'Chimie' },
  web: { id: '66666666-6666-6666-6666-666666666666', name: 'Cristian Stoica', category: 'Web Development' },
  ml: { id: '77777777-7777-7777-7777-777777777777', name: 'Diana Munteanu', category: 'Machine Learning' },
  databases: { id: '88888888-8888-8888-8888-888888888888', name: 'Mihai Enache', category: 'Baze de Date' },
  german: { id: '99999999-9999-9999-9999-999999999999', name: 'Stefan Weber', category: 'Limba Germana' },
  economics: { id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', name: 'Laura Gheorghe', category: 'Economie' },
  biology: { id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Simona Stan', category: 'Biologie' }
} as const;

type TutorKey = keyof typeof TUTORS;

const evaluationCase = (tutor: TutorKey, query: string): EvaluationCase => ({
  query,
  expectedTutorId: TUTORS[tutor].id,
  expectedTutorName: TUTORS[tutor].name,
  category: TUTORS[tutor].category
});

export const EVALUATION_CASES: readonly EvaluationCase[] = [
  evaluationCase('math', 'Am nevoie de ajutor urgent cu analiza matematica, in special derivate si integrale duble'),
  evaluationCase('math', 'Vreau sa inteleg algebra liniara, spatii vectoriale, vectori proprii si matrici'),
  evaluationCase('math', 'Caut meditator pentru geometrie analitica si functii trigonometrice'),
  evaluationCase('algorithms', 'Vreau sa invat algoritmi si structuri de date pentru interviuri tehnice FAANG'),
  evaluationCase('algorithms', 'Am dificultati la programare orientata pe obiecte in C++ si Java'),
  evaluationCase('algorithms', 'Caut cineva sa-mi explice probleme de grafuri, Dijkstra si programare dinamica'),
  evaluationCase('physics', 'Caut profesor pentru fizica de liceu si facultate, capitolele de termodinamica si electromagnetism'),
  evaluationCase('physics', 'Am nevoie de explicatii pentru ecuatia lui Schrodinger si mecanica cuantica'),
  evaluationCase('physics', 'Ajutor la probleme de optica ondulatorie si mecanica fluidelor'),
  evaluationCase('english', 'Pregatire intensiva si simulari pentru examenul Cambridge C1 Advanced'),
  evaluationCase('english', 'Vreau sa iau un scor mare la IELTS Academic pentru a studia in strainatate'),
  evaluationCase('english', 'Cursuri de conversatie fluenta in limba engleza pentru afaceri si redactare de eseuri'),
  evaluationCase('chemistry', 'Sinteze de chimie organica si mecanisme de reactie pentru admiterea la medicina UMF'),
  evaluationCase('chemistry', 'Am nevoie de pregatire la chimie si compusi organici pentru examenul de admitere'),
  evaluationCase('web', 'Cum sa construiesc aplicatii web moderne de la zero folosind React si TypeScript'),
  evaluationCase('web', 'Am nevoie de mentorat pentru crearea unui backend REST API cu Node.js si Express'),
  evaluationCase('web', 'Vreau sa inteleg frontend state management, hooks si optimizare de performanta'),
  evaluationCase('ml', 'Cursuri practice de Machine Learning cu Python, regresie liniara, clasificare si retele neuronale'),
  evaluationCase('ml', 'Vreau sa invat procesarea limbajului natural, tokenizare si modele transformer'),
  evaluationCase('ml', 'Data science si analiza statistica folosind librariile pandas, numpy si matplotlib'),
  evaluationCase('databases', 'Optimizare de query-uri SQL complexe, planuri de executie si indecsi in baze relationale'),
  evaluationCase('databases', 'Modelare de tabele, chei straine si normalizare a bazelor de date in PostgreSQL'),
  evaluationCase('german', 'Vreau sa invat limba germana de la incepator pana la nivel mediu B2 pentru un job in Germania'),
  evaluationCase('german', 'Pregatire specifica pentru promovarea certificatului Goethe Zertifikat B2'),
  evaluationCase('economics', 'Explicatii la economie, cerere si oferta, politica monetara si finante corporative'),
  evaluationCase('biology', 'Meditatii la biologie pentru bacalaureat: genetica moleculara, legile lui Mendel si anatomie umana')
];
